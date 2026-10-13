using TMPro;
using UnityEngine;

[RequireComponent(typeof(PlayerIdentity), typeof(FPSController), typeof(BallHandler))]
[RequireComponent(typeof(PlayerHealth))]
public class DodgeballPlayer : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private GameObject bodyVisual;
    [SerializeField] private GameObject hudRoot;
    [Tooltip("Movement speed while winding up or holding a throw. 0 = stand still, 1 = full speed.")]
    [SerializeField, Range(0f, 1f)] private float throwingSpeedMultiplier = 0.5f;
    [Tooltip("How strongly the player color tints the body.")]
    [SerializeField, Range(0f, 1f)] private float bodyTint = 0.7f;
    [SerializeField] private float nameTagHeight = 2.5f;

    private CharacterController characterController;
    private bool isNetworkView;
    private bool isPredicted;
    private bool matchActive = true;
    private int previousLives = -1;
    private float predictedHoldUntil;
    private BallSpawner ballSpawner;
    private IPlayerInputSource localInput;

    private const float PickupPredictionTime = 0.5f;
    private DodgeballMatchController match;
    private PlayerStateData networkState = new PlayerStateData { grounded = true };
    private TextMeshPro nameTag;

    public static Camera ViewCamera { get; private set; }

    public PlayerIdentity Identity { get; private set; }
    public FPSController Movement { get; private set; }
    public BallHandler Balls { get; private set; }
    public PlayerHealth Health { get; private set; }
    public int PlayerId => Identity.PlayerId;

    private bool UsesSnapshotMovement => isNetworkView && !isPredicted;

    public bool IsHoldingBall => isNetworkView ? networkState.holdingBall || Time.time < predictedHoldUntil : Balls.IsHolding;
    public bool IsCrouching => UsesSnapshotMovement ? networkState.crouching : Movement.IsCrouching;
    public bool IsGrounded => UsesSnapshotMovement ? networkState.grounded : Movement.IsGrounded;
    public float ThrowProgress => isNetworkView ? networkState.throwProgress : Balls.ThrowProgress;
    public float ThrowCharge => isNetworkView ? networkState.throwCharge : Balls.ThrowCharge;
    public int LastInputSequence => TryGetComponent(out RemoteInputSource remote) ? remote.LastProcessedSequence : 0;
    private bool IsThrowing => isNetworkView ? networkState.throwProgress > 0f : Balls.IsThrowing;

    private void Awake()
    {
        Identity = GetComponent<PlayerIdentity>();
        Movement = GetComponent<FPSController>();
        Balls = GetComponent<BallHandler>();
        Health = GetComponent<PlayerHealth>();
        characterController = GetComponent<CharacterController>();
        Health.OnLivesChanged += HandleLivesChanged;
        CreateNameTag();
    }

    private void OnDestroy()
    {
        Health.OnLivesChanged -= HandleLivesChanged;
    }

    private void Update()
    {
        if (isPredicted)
            PredictPickup();

        if (!UsesSnapshotMovement)
            Movement.SpeedMultiplier = IsThrowing ? throwingSpeedMultiplier : 1f;
    }

    private void LateUpdate()
    {
        if (nameTag != null && ViewCamera != null)
            nameTag.transform.rotation = ViewCamera.transform.rotation;
    }

    public void ApplyIdentityVisuals()
    {
        Color color = Identity.Color;
        Color tint = Color.Lerp(Color.white, color, bodyTint);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer bodyRenderer in bodyVisual.GetComponentsInChildren<Renderer>(true))
        {
            if (nameTag != null && bodyRenderer.gameObject == nameTag.gameObject)
                continue;

            bodyRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, tint);
            block.SetColor(ColorId, tint);
            bodyRenderer.SetPropertyBlock(block);
        }

        if (nameTag != null)
        {
            nameTag.text = Identity.DisplayName;
            nameTag.color = color;
        }
    }

    public void ConfigureAsLocal(KeyboardProfile profile, Rect viewport, bool enableAudio, string bodyLayerName)
    {
        gameObject.AddComponent<LocalKeyboardInput>().SetProfile(profile);
        EnableView(viewport, enableAudio, bodyLayerName);
    }

    public void ConfigureAsRemote()
    {
        gameObject.AddComponent<RemoteInputSource>();
        DisableView();
    }

    public void ConfigureAsNetworkView(bool isLocalPlayer, KeyboardProfile profile, string bodyLayerName)
    {
        isNetworkView = true;
        Balls.enabled = false;

        NetworkPlayerView view = gameObject.AddComponent<NetworkPlayerView>();
        if (!isLocalPlayer)
        {
            Movement.enabled = false;
            characterController.enabled = false;
            DisableView();
            return;
        }

        isPredicted = true;
        view.EnablePrediction();
        gameObject.AddComponent<LocalKeyboardInput>().SetProfile(profile);
        gameObject.AddComponent<NetworkInputSender>();
        EnableView(new Rect(0f, 0f, 1f, 1f), true, bodyLayerName);
    }

    public void ApplyNetworkState(PlayerStateData state)
    {
        networkState = state;
        if (state.holdingBall)
            predictedHoldUntil = 0f;
    }

    public void ResetForRound(Vector3 spawnPosition, int maxLives)
    {
        Balls.ReleaseBall();
        Movement.Teleport(spawnPosition);
        Health.ResetLives(maxLives);
    }

    public void SetMatchActive(bool isActive)
    {
        matchActive = isActive;
        Health.AcceptsHits = isActive;
        RefreshActions();
    }

    private void RefreshActions()
    {
        bool canAct = matchActive && !Health.IsEliminated;
        Movement.CanMove = canAct;
        Balls.CanAct = canAct;
    }

    private void HandleLivesChanged(int lives)
    {
        if (previousLives >= 0 && lives < previousLives && !IsRoundStarting())
            UIAudio.PlayImpactAt(transform.position + Vector3.up);
        previousLives = lives;

        bool alive = lives > 0;
        RefreshActions();
        if (!alive)
            Balls.ReleaseBall();

        bodyVisual.SetActive(alive);
        characterController.detectCollisions = alive;
    }

    private void PredictPickup()
    {
        if (localInput == null)
            localInput = GetComponent<IPlayerInputSource>();
        if (localInput == null || !localInput.Current.pickupPressed || IsHoldingBall || !Movement.CanMove)
            return;

        if (ballSpawner == null)
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        if (ballSpawner == null)
            return;

        DodgeBall closest = ballSpawner.FindClosestPickable(Balls.HoldPoint.position, Balls.PickupRadius, PlayerId);
        if (closest == null)
            return;

        closest.PredictHold(Balls.HoldPoint, PickupPredictionTime);
        predictedHoldUntil = Time.time + PickupPredictionTime;
    }

    private bool IsRoundStarting()
    {
        if (match == null)
            match = FindFirstObjectByType<DodgeballMatchController>();
        return match != null && (match.Phase == MatchPhase.Countdown || match.Phase == MatchPhase.Waiting);
    }

    private void EnableView(Rect viewport, bool enableAudio, string bodyLayerName)
    {
        playerCamera.enabled = true;
        playerCamera.rect = viewport;
        audioListener.enabled = enableAudio;
        hudRoot.SetActive(true);
        HideOwnBody(bodyLayerName);

        if (ViewCamera == null || !ViewCamera.isActiveAndEnabled)
            ViewCamera = playerCamera;
    }

    private void DisableView()
    {
        playerCamera.enabled = false;
        audioListener.enabled = false;
        hudRoot.SetActive(false);
    }

    private void CreateNameTag()
    {
        if (bodyVisual == null)
            return;

        GameObject tagObject = new GameObject("NameTag");
        tagObject.transform.SetParent(bodyVisual.transform, false);
        tagObject.transform.position = transform.position + Vector3.up * nameTagHeight;

        nameTag = tagObject.AddComponent<TextMeshPro>();
        nameTag.fontSize = 3f;
        nameTag.alignment = TextAlignmentOptions.Center;
        nameTag.fontStyle = FontStyles.Bold;
        nameTag.rectTransform.sizeDelta = new Vector2(4f, 1f);
        nameTag.outlineWidth = 0.2f;
        nameTag.outlineColor = Color.black;
    }

    private void HideOwnBody(string bodyLayerName)
    {
        int layer = LayerMask.NameToLayer(bodyLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"Layer '{bodyLayerName}' does not exist. Create it in Project Settings > Tags and Layers.");
            return;
        }

        foreach (Transform child in bodyVisual.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;

        playerCamera.cullingMask &= ~(1 << layer);
    }
}
