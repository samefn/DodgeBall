using System.Net;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private const int MaxRoomNameLength = 20;
    private const int SearchStepMs = 100;

    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Optional: lists the rooms found on the local network.")]
    [SerializeField] private TMP_Text roomsText;
    [SerializeField] private int port = 5555;
    [Tooltip("Seconds a client keeps looking for a room before giving up.")]
    [SerializeField] private float roomSearchTimeout = 3f;

    [Header("Panels (optional)")]
    [Tooltip("Jugar / Jugar local / Configuración / Salir.")]
    [SerializeField] private GameObject mainPanel;
    [Tooltip("Room name, Crear sala, Unirse a la sala.")]
    [SerializeField] private GameObject roomsPanel;
    [SerializeField] private GameObject settingsPanel;

    private NetworkService network;
    private bool isBusy;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (statusText == null)
            Debug.LogWarning("[Menu] Status Text is not assigned in MainMenuUI; players will not see connection messages.");

        roomNameInput.characterLimit = MaxRoomNameLength;
        InputGate.Blocked = false;

        string reason = GameSession.LastDisconnectReason;
        GameSession.LastDisconnectReason = null;
        if (string.IsNullOrEmpty(reason))
            ShowMain();
        else
            ShowRooms();
        SetStatus(reason ?? string.Empty);

        network = NetworkService.Instance;
        if (network == null || network.Discovery == null)
        {
            SetStatus("No se encontró el servicio de red.");
            return;
        }

        network.Discovery.OnRoomsChanged += RefreshRoomList;
        network.Discovery.StartListening();
        RefreshRoomList();
    }

    private void OnDestroy()
    {
        if (network == null || network.Discovery == null)
            return;

        network.Discovery.OnRoomsChanged -= RefreshRoomList;
        network.Discovery.StopListening();
    }

    public void ShowMain()
    {
        ShowPanel(mainPanel);
    }

    public void ShowRooms()
    {
        ShowPanel(roomsPanel);
    }

    public void ShowSettings()
    {
        ShowPanel(settingsPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
            return;

        if (mainPanel != null)
            mainPanel.SetActive(panel == mainPanel);
        if (roomsPanel != null)
            roomsPanel.SetActive(panel == roomsPanel);
        if (settingsPanel != null)
            settingsPanel.SetActive(panel == settingsPanel);
    }

    public void PlayLocal()
    {
        GameSession.Mode = GameMode.Local;
        SceneManager.LoadScene(SceneNames.DodgeballArena);
    }

    public async void CreateRoom()
    {
        if (isBusy || !IsNetworkReady() || !TryReadRoomName(out string roomName))
            return;

        if (network.Discovery.TryGetRoom(roomName, out _))
        {
            SetStatus($"Ya existe una sala llamada \"{roomName}\". Elige otro nombre.");
            return;
        }

        isBusy = true;
        SetStatus($"Creando la sala \"{roomName}\"...");
        bool started = await network.Host(port, roomName);
        isBusy = false;

        if (!started)
        {
            SetStatus($"No se pudo crear la sala. ¿Hay otra sala abierta en este PC usando el puerto {port}?");
            return;
        }

        GameSession.Mode = GameMode.Host;
        SceneManager.LoadScene(SceneNames.Lobby);
    }

    public async void JoinRoom()
    {
        if (isBusy || !IsNetworkReady() || !TryReadRoomName(out string roomName))
            return;

        isBusy = true;
        bool joined = IPAddress.TryParse(roomName, out _)
            ? await JoinByAddress(roomName)
            : await JoinByRoomName(roomName);
        isBusy = false;

        if (!joined)
            return;

        GameSession.Mode = GameMode.Client;
        SceneManager.LoadScene(SceneNames.Lobby);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private async Task<bool> JoinByRoomName(string roomName)
    {
        SetStatus($"Buscando la sala \"{roomName}\"...");
        RoomInfo room = await WaitForRoom(roomName);

        if (room == null)
        {
            SetStatus($"No se encontró la sala \"{roomName}\". Revisa el nombre y la red. Si el host está en otro PC, puede que el firewall de ESTE PC bloquee el aviso de salas (UDP 47777): ejecuta Firewall-Dodgeball.bat aquí o escribe la IP del host.");
            return false;
        }

        if (room.IsFull)
        {
            SetStatus($"La sala \"{room.RoomName}\" está llena ({room.PlayerCount}/{room.MaxPlayers}).");
            return false;
        }

        SetStatus($"Conectando a la sala \"{room.RoomName}\"...");
        if (await network.Join(room.Address, room.Port, room.RoomName))
            return true;

        SetStatus($"La sala existe pero el PC del host ({room.Address}) bloqueó la conexión (TCP {room.Port}). Ejecuta Firewall-Dodgeball.bat en el PC del host.");
        return false;
    }

    private async Task<bool> JoinByAddress(string address)
    {
        SetStatus($"Conectando a {address}...");
        if (await network.Join(address, port, address))
            return true;

        SetStatus($"No se pudo conectar a {address}:{port}. Revisa la IP, que el host haya creado la sala y ejecuta Firewall-Dodgeball.bat en el PC del host.");
        return false;
    }

    private async Task<RoomInfo> WaitForRoom(string roomName)
    {
        float deadline = Time.realtimeSinceStartup + roomSearchTimeout;
        while (true)
        {
            if (network.Discovery.TryGetRoom(roomName, out RoomInfo room))
                return room;
            if (Time.realtimeSinceStartup >= deadline)
                return null;

            await Task.Delay(SearchStepMs);
        }
    }

    private bool TryReadRoomName(out string roomName)
    {
        roomName = roomNameInput.text.Trim();
        if (roomName.Length > 0)
            return true;

        SetStatus("Escribe el nombre de la sala.");
        return false;
    }

    private bool IsNetworkReady()
    {
        if (network != null && network.Discovery != null)
            return true;

        SetStatus("No se encontró el servicio de red.");
        return false;
    }

    private void RefreshRoomList()
    {
        if (roomsText == null)
            return;

        if (network.Discovery.Rooms.Count == 0)
        {
            roomsText.text = "No hay salas abiertas en tu red.";
            return;
        }

        StringBuilder list = new StringBuilder("Salas disponibles:\n");
        foreach (RoomInfo room in network.Discovery.Rooms)
            list.AppendLine($"{room.RoomName}  ({room.PlayerCount}/{room.MaxPlayers})");
        roomsText.text = list.ToString();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log($"[Menu] {message}");
    }
}
