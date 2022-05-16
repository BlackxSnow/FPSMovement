using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Controls;
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Player;
    public static FPSControls PlayerInput;
    #region Modifier Variables
    /// <summary>
    /// Degrees the camera is tilted during wall running
    /// </summary>
    public float WallRunTiltDegrees = 10;
    /// <summary>
    /// Degrees per second the camera is tilted to target
    /// </summary>
    public float TiltSpeed = 60;
    /// <summary>
    /// How many degrees the camera tilts by when crouching
    /// </summary>
    public float CrouchTiltDegrees = 10;
    /// <summary>
    /// Speed of tilting the camera when crouching
    /// </summary>
    public float CrouchCameraSpeed = 10;
    /// <summary>
    /// How far the camera moves downwards when crouching
    /// </summary>
    public float CrouchCameraDistance = 0.75f;


    /// <summary>
    /// Input multiplier for maneuvering in the air
    /// </summary>
    public float AirSpeedMultiplier = 2.5f;
    /// <summary>
    /// Modifier on JumpForce for a wall jump's horizontal speed
    /// </summary>
    public float WallJumpModifier = 1f;

    /// <summary>
    /// Camera movement sensitivity
    /// </summary>
    public float Sensitivity = 0.1f;
    /// <summary>
    /// Base max movement velocity
    /// </summary>
    public float Speed = 5f;
    /// <summary>
    /// Max velocity during sprint
    /// </summary>
    public float SprintSpeed = 15f;
    /// <summary>
    /// Whether the player can increase their maximum velocity in the air by sprinting
    /// </summary>
    public bool CanSprintInAir = false;
    /// <summary>
    /// Acceleration applied to movement
    /// </summary>
    public float Acceleration = 200f;
    /// <summary>
    /// Degree limit which the player can walk on and be considered grounded
    /// </summary>
    public float GroundAngleLimit = 60f;
    /// <summary>
    /// Degree limit between the player forward and movement direction that sprint can be used in
    /// </summary>
    public float SprintAngleLimit = 60;
    /// <summary>
    /// Epsilon value for the distance at which the player can still be considered grounded
    /// </summary>
    public float GroundedThreshold = 0.05f;
    /// <summary>
    /// The vertical velocity given to player on jump
    /// </summary>
    public float JumpForce = 10f;
    /// <summary>
    /// Minimum time after a jump action that actor cannot be grounded
    /// </summary>
    public float JumpMinAirTime = 0.01f;
    /// <summary>
    /// Multiplier for how much velocity is retained when wall jumping
    /// </summary>
    public float WallJumpVelocityRetention = 0.75f;
    /// <summary>
    /// Whether the player's vertical velocity is retained when jumping from a wall
    /// </summary>
    public bool WallJumpRetainY = false;
    /// <summary>
    /// Whether the wall jump is based on the player's look direction
    /// </summary>
    public bool WallJumpInLookDirection = false;
    /// <summary>
    /// Bezier curve for mapping player vertical look angle to wall jump vertical direction
    /// </summary>
    public AnimationCurve WallJumpAngleCurve;


    /// <summary>
    /// Limit on how many discrete wall runs can be performed before touching the ground again. -1 for infinite
    /// </summary>
    public int WallRunLimit = -1;
    /// <summary>
    /// The minimum velocity angle from the gravitational direction to maintain a wallrun
    /// </summary>
    public float WallRunMinVelocityAngleFromDown = 30;
    /// <summary>
    /// Multiplier for vertical velocity when beginning a wallrun
    /// </summary>
    public float WallRunYVelocityBonus = 1;
    /// <summary>
    /// Multiplier for gravitational effect while wallrunning
    /// </summary>
    public float WallRunGravityModifier = 0.5f;
    /// <summary>
    /// Whether holding space (jump) is required to begin a wallrun
    /// </summary>
    public bool SpaceToStartWallRun = false;
    /// <summary>
    /// Whether user input is relative to the look direction or the current horizontal velocity direction
    /// </summary>
    public bool IsInputOnWallVelocityAligned = true;
    /// <summary>
    /// Maximum degree difference between horizontal velocity and movement input before wall run is ended
    /// </summary>
    const float WallRunMaxInputAngle = 45;
    /// <summary>
    /// Friction while sliding
    /// </summary>
    public float SlideFriction = 1.5f;
    /// <summary>
    /// Base friction (while no input is applied)
    /// </summary>
    public float BaseFriction = 10f;
    /// <summary>
    /// How quickly friction changes apply
    /// </summary>
    public float FrictionGainTime = 0.5f;
    /// <summary>
    /// Modifier on velocity drag while in the air
    /// </summary>
    public float AerialDragCoefficient = 0.2f;
    #endregion

    //Vector3 WallRunStartVelocity;
    Vector3 CameraBasePosition;

    /// <summary>
    /// Wall runs performed this air time
    /// </summary>
    private int WallRunCount;
    Vector3 LastWallRunNormal;
    Collider LastWallRunCollider;
    bool IsWallRunning;
    /// <summary>
    /// Horizontal direction of the current wallrun
    /// </summary>
    Vector3 WallRunForward;
    /// <summary>
    /// Up direction of the current wallrun
    /// </summary>
    Vector3 WallRunUp;
    bool IsWallJumping;

    public Transform HeadCamera;
    private Transform TiltObject;
    private Rigidbody PlayerRigidBody;
    private Collider PlayerCollider;

    private bool IsGroundedCollision;
    public bool IsGrounded { get { return IsGroundedCollision; } set { IsGroundedCollision = value; } }

    public Vector3 GroundNormal;
    public bool IsOnWall { get; private set; }
    public Vector3 WallNormal;
    private Collider WallCollider;
    public bool IsOnSurface { get { return IsOnWall || IsGrounded; } }

    private bool IsJumpingNextFrame;
    private float TimeSinceLastJump;
    private float TimeSinceAerial;

    private void Awake()
    {
        TiltObject = transform.GetChild(0);
        HeadCamera = TiltObject.GetChild(0);
        CameraBasePosition = HeadCamera.localPosition;
        PlayerRigidBody = GetComponent<Rigidbody>();
        PlayerCollider = GetComponent<Collider>();
        PlayerInput = new FPSControls();
        PlayerInput.Enable();
        Player = this;
    }

    private void Start()
    {
        PlayerInput.Player.Jump.started += UserJump;
        CreateVariableInputFields();
    }

    /// <summary>
    /// Wrapper for player velocity
    /// </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return PlayerRigidBody.velocity;
    }

    /// <summary>
    /// Initialise variable modification UI fields
    /// </summary>
    private void CreateVariableInputFields()
    {
        UIManager ui = UIManager.Instance;
        //ui.CreateVariableInput<float>(() => { return WallRunTiltDegrees.ToString(); }, (s) => { WallRunTiltDegrees = float.Parse(s); }, "TiltDegrees", "//Degree tilt during wall run");
        //ui.CreateVariableInput<float>(() => { return TiltSpeed.ToString(); }, (s) => { TiltSpeed = float.Parse(s); }, "TiltSpeed", "//Degrees per second to tilt to target rotation");
        //ui.CreateVariableInput<float>(() => { return CrouchTiltDegrees.ToString(); }, (s) => { CrouchTiltDegrees = float.Parse(s); }, "CrouchTiltDegrees", "//Degree tilt during slide");

        ui.CreatePanelGroup("General");
        ui.CreateVariableInput<float, string>(() => { return Sensitivity.ToString(); }, (s) => { Sensitivity = float.Parse(s); }, "Sensitivity", "//Mouse sensitivity for rotation");
        ui.CreateVariableInput<float, string>(() => { return Physics.gravity.y.ToString(); }, (s) => { Physics.gravity = new Vector3(0, float.Parse(s), 0); }, "Gravity.y", "//Vertical gravity. Probably fucked at positive");
        ui.CreateVariableInput<float, string>(() => { return Speed.ToString(); }, (s) => { Speed = float.Parse(s); }, "Speed", "//Base max ground speed");
        ui.CreateVariableInput<float, string>(() => { return SprintSpeed.ToString(); }, (s) => { SprintSpeed = float.Parse(s); }, "SprintSpeed", "//Sprint max ground speed");
        ui.CreateVariableInput<bool, bool>(() => { return CanSprintInAir; }, (s) => { CanSprintInAir = s; }, "CanSprintInAir", "//Is the player able to sprint while in the air");
        ui.CreateVariableInput<float, string>(() => { return Acceleration.ToString(); }, (s) => { Acceleration = float.Parse(s); }, "Acceleration", "//Movement delta per second on ground");

        ui.CreatePanelGroup("Angles");
        ui.CreateVariableInput<float, string>(() => { return GroundAngleLimit.ToString(); }, (s) => { GroundAngleLimit = float.Parse(s); }, "GroundAngleLimit", "//Max angle of walkable ground");
        ui.CreateVariableInput<float, string>(() => { return SprintAngleLimit.ToString(); }, (s) => { SprintAngleLimit = float.Parse(s); }, "SprintAngleLimit", "//Max angle from forward where sprint is valid");

        ui.CreatePanelGroup("Jump/Aerial");
        //ui.CreateVariableInput<float>(() => { return GroundedThreshold.ToString(); }, (s) => { GroundedThreshold = float.Parse(s); }, "GroundedThreshold", "//Max distance from ground where player is grounded");
        ui.CreateVariableInput<float, string>(() => { return AirSpeedMultiplier.ToString(); }, (s) => { AirSpeedMultiplier = float.Parse(s); }, "AirSpeedMultiplier", "//Input multiplier while airborne");
        //ui.CreateVariableInput<float>(() => { return JumpMinAirTime.ToString(); }, (s) => { JumpMinAirTime = float.Parse(s); }, "JumpMinAirTime", "//Min time after jump that actor cannot be grounded");
        ui.CreateVariableInput<float, string>(() => { return JumpForce.ToString(); }, (s) => { JumpForce = float.Parse(s); }, "JumpForce", "//Vertical velocity given on jump");

        ui.CreatePanelGroup("Wall Run/Jump");
        ui.CreateVariableInput<float, string>(() => { return WallJumpModifier.ToString(); }, (s) => { WallJumpModifier = float.Parse(s); }, "WallJumpModifier", "//Wall Jump force modifier");
        ui.CreateVariableInput<float, string>(() => { return WallJumpVelocityRetention.ToString(); }, (s) => { WallJumpVelocityRetention = float.Parse(s); }, "WallJumpVelocityRetention", "//Velocity retention on wall jump");
        ui.CreateVariableInput<bool, bool>(() => { return WallJumpRetainY; }, (s) => { WallJumpRetainY = s; }, "WallJumpRetainY", "//Does velocity retention include vertical velocity?");
        ui.CreateVariableInput<bool, bool>(() => { return WallJumpInLookDirection; }, (s) => { WallJumpInLookDirection = s; }, "WallJumpInLookDirection", "//Wall jump direction follows look direction");
        ui.CreateVariableInput<int, string>(() => { return WallRunLimit.ToString(); }, (s) => { WallRunLimit = int.Parse(s); }, "WallRunLimit", "//Wall runs that can be performed before touching the ground");
        
        ui.CreateVariableInput<float, string>(() => { return WallRunGravityModifier.ToString(); }, (s) => { WallRunGravityModifier = float.Parse(s); }, "WallRunGravityModifier", "//Amount of gravity applied while wall running");
        ui.CreateVariableInput<float, string>(() => { return WallRunYVelocityBonus.ToString(); }, (s) => { WallRunYVelocityBonus = float.Parse(s); }, "WallRunYVelocityBonus", "//Max amount of JumpForce added on starting wall run");
        //ui.CreateVariableInput<float, string>(() => { return WallRunMaxTime.ToString(); }, (s) => { WallRunMaxTime = float.Parse(s); }, "WallRunMaxTime", "//Max time on a single wall run");
        //ui.CreateVariableInput<float, string>(() => { return WallRunMinSpeed.ToString(); }, (s) => { WallRunMinSpeed = float.Parse(s); }, "WallRunMinSpeed", "//Min horizontal velocity to wall run");
        ui.CreateVariableInput<bool, bool>(() => { return SpaceToStartWallRun; }, (s) => { SpaceToStartWallRun = s; }, "SpaceToStartWallRun", "//Require space to be held to begin a wall run");
        
        ui.CreateVariableInput<bool, bool>(() => { return IsInputOnWallVelocityAligned; }, (s) => { IsInputOnWallVelocityAligned = s; }, "IsInputOnWallVelocityAligned", "//If true, W always points towards velocity. Will stop you popping of the wall by looking around");

        ui.CreatePanelGroup("Physics");
        ui.CreateVariableInput<float, string>(() => { return BaseFriction.ToString(); }, (s) => { BaseFriction = float.Parse(s); }, "BaseFriction", "//Friction while input is 0 on ground");
        ui.CreateVariableInput<float, string>(() => { return FrictionGainTime.ToString(); }, (s) => { FrictionGainTime = float.Parse(s); }, "FrictionGainTime", "//Time after landing it takes to reach full friction");
        ui.CreateVariableInput<float, string>(() => { return SlideFriction.ToString(); }, (s) => { SlideFriction = float.Parse(s); }, "SlideFriction", "//Friction while sliding on ground");
        ui.CreateVariableInput<float, string>(() => { return AerialDragCoefficient.ToString(); }, (s) => { AerialDragCoefficient = float.Parse(s); }, "AerialDragCoefficient", "//Percent loss of horizontal velocity per second while aerial");
    }

    private void Update()
    {
        PerformRotation();
    }

    void FixedUpdate()
    {
        PerformMovement();
    }

    //x corresponds to left-right rotation, y corresponds to up-down
    Vector2 CurrentRotation;
    /// <summary>
    /// Get user input and rotate the player + camera
    /// </summary>
    void PerformRotation()
    {
        if(UIManager.IsPaused)
        {
            return;
        }
        Vector2 input = PlayerInput.Player.Rotation.ReadValue<Vector2>();
        input.y *= -1;

        CurrentRotation.x += input.x * Sensitivity;
        CurrentRotation.y += input.y * Sensitivity;
        CurrentRotation.y = Mathf.Clamp(CurrentRotation.y, -89, 89);

        transform.rotation = Quaternion.Euler(0, CurrentRotation.x, 0);
        HeadCamera.localRotation = Quaternion.Euler(CurrentRotation.y, 0, 0);
    }

    /// <summary>
    /// Determine current relevant collision states for wall run / jumping validity
    /// </summary>
    /// <param name="collision"></param>
    private void GetCollisionStates(Collision collision)
    {
        IsOnWall = false;
        IsGrounded = false;
        WallCollider = null;
        foreach (ContactPoint contact in collision.contacts)
        {
            float angle = Vector3.Angle(Vector3.up, contact.normal);
            if (angle < GroundAngleLimit && TimeSinceLastJump > JumpMinAirTime)
            {
                GroundNormal = contact.normal;
                IsGrounded = true;
            }
            else if (angle <= 95 && angle > GroundAngleLimit)
            {
                IsOnWall = true;
                WallNormal = contact.normal;
                WallCollider = contact.otherCollider;
            }
        }
        if(!IsGrounded && IsOnWall)
        {
            Ray ray = new Ray(PlayerCollider.bounds.center, Vector3.down);
            if(Physics.Raycast(ray, out RaycastHit hit, GroundedThreshold + PlayerCollider.bounds.extents.y))
            {
                if(Vector3.Angle(Vector3.up, hit.normal) < GroundAngleLimit)
                {
                    GroundNormal = hit.normal;
                    IsGrounded = true;
                }
            }
        }
        SetFrictionStates();
    }

    /// <summary>
    /// Set character friction based on current status
    /// </summary>
    private void SetFrictionStates()
    {
        bool isCrouching = PlayerInput.Player.Crouch.ReadValue<float>() > 0;
        bool isInputZero = UIManager.IsPaused ? true : PlayerInput.Player.Movement.ReadValue<Vector2>().magnitude < 0.01f;
        if (isCrouching && IsGrounded)
        {
            SetFriction(Mathf.Min(TimeSinceAerial * SlideFriction / FrictionGainTime, SlideFriction));
        }
        else if (IsGrounded && isInputZero)
        {
            SetFriction(Mathf.Min(TimeSinceAerial * BaseFriction / FrictionGainTime, BaseFriction));
        }
        else
        {
            SetFriction(0);
        }
    }

    //On collision change, check current states
    private void OnCollisionEnter(Collision c) => GetCollisionStates(c);
    private void OnCollisionExit(Collision c) => GetCollisionStates(c);
    private void OnCollisionStay(Collision collision) => GetCollisionStates(collision);

    /// <summary>
    /// Begin a new wallrun
    /// </summary>
    /// <param name="forward"></param>
    /// <param name="up"></param>
    private void StartWallRun(Vector3 forward, Vector3 up)
    {
        IsWallRunning = true;
        WallRunCount++;
        PlayerRigidBody.useGravity = false;

        float verticalMultiplier = Mathf.Clamp(Utility.ScalarProjection(up, Vector3.up) + Utility.ScalarProjection(forward, Vector3.up), 0, 1);
        PlayerRigidBody.velocity += (forward + up).normalized * verticalMultiplier * JumpForce * WallRunYVelocityBonus;
    }

    /// <summary>
    /// Stop an existing wallrun
    /// </summary>
    private void EndWallRun()
    {
        IsWallRunning = false;
        PlayerRigidBody.useGravity = true;
        LastWallRunCollider = WallCollider;
        LastWallRunNormal = WallNormal;
    }

    /// <summary>
    /// Receive input and perform resultant modifications
    /// </summary>
    void PerformMovement()
    {
        TimeSinceLastJump += Time.fixedDeltaTime;
        IsWallJumping = false;

        Vector2 input = UIManager.IsPaused ? Vector2.zero : PlayerInput.Player.Movement.ReadValue<Vector2>();
        Vector3 modifiedMovement;

        if (IsWallRunning && (!IsOnWall || IsGrounded))
        {
            EndWallRun();
            Debug.Log("Ending Wall Run - No longer on wall");
        }

        TimeSinceAerial = IsGrounded ? TimeSinceAerial + Time.fixedDeltaTime : 0;

        
        if(IsWallRunning && IsInputOnWallVelocityAligned)
        {
            modifiedMovement = new Vector3(input.x, 0, input.y);
            modifiedMovement = modifiedMovement.normalized;
        }
        else
        {
            modifiedMovement = transform.localToWorldMatrix.MultiplyVector(new Vector3(input.x, 0, input.y));
            modifiedMovement = modifiedMovement.normalized;
        }

        if (IsGrounded)
        {
            modifiedMovement = Vector3.ProjectOnPlane(modifiedMovement, GroundNormal);
            WallRunCount = 0;
            LastWallRunCollider = null;
        }
        else if (IsOnWall)
        {
            DoWallRunCheck(modifiedMovement);
        }
        else
        {
            modifiedMovement *= AirSpeedMultiplier;
            float dragMultiplier = 1 - (AerialDragCoefficient * Time.fixedDeltaTime);
            PlayerRigidBody.velocity = new Vector3(PlayerRigidBody.velocity.x * dragMultiplier, PlayerRigidBody.velocity.y, PlayerRigidBody.velocity.z * dragMultiplier);
        }

        bool isSprinting = PlayerInput.Player.Sprint.ReadValue<float>() > 0 && Vector3.Angle(modifiedMovement, transform.forward) < SprintAngleLimit;
        modifiedMovement *= isSprinting && (IsGrounded || CanSprintInAir) ? SprintSpeed : Speed;

        bool isJumpingNow = false;
        if (IsJumpingNextFrame && IsOnSurface)
        {
            modifiedMovement = GetJumpVector(modifiedMovement, out isJumpingNow);
        }
        IsJumpingNextFrame = false;


        bool isCrouching = PlayerInput.Player.Crouch.ReadValue<float>() > 0 && IsGrounded;
        DoCameraEffects(PlayerRigidBody.velocity.magnitude, isCrouching);

        ChangeVelocity(modifiedMovement, isCrouching, isJumpingNow);
    }
    /// <summary>
    /// Perform friction-esque velocity modifications based on character status
    /// </summary>
    /// <param name="movement"></param>
    /// <param name="isCrouching"></param>
    /// <param name="isJumping"></param>
    private void ChangeVelocity(Vector3 movement, bool isCrouching, bool isJumping)
    {
        if (IsGrounded)
        {
            if (isCrouching || movement.magnitude == 0)
            {
                return;
            }

            Vector3 deltaVelocity = movement - PlayerRigidBody.velocity;
            if (isJumping)
            {
                deltaVelocity.y = 0;
                PlayerRigidBody.AddForce(Vector3.up * movement.y, ForceMode.VelocityChange);
            }
            deltaVelocity = Utility.MinByAbsolute(deltaVelocity.normalized * Acceleration * Time.fixedDeltaTime, deltaVelocity);
            PlayerRigidBody.AddForce(deltaVelocity, ForceMode.VelocityChange);

        }
        else if (IsWallRunning)
        {
            PlayerRigidBody.velocity += Physics.gravity * Time.fixedDeltaTime * WallRunGravityModifier;
        }
        else if (IsWallJumping)
        {
            if (WallJumpRetainY)
            {
                PlayerRigidBody.velocity = movement + (PlayerRigidBody.velocity * WallJumpVelocityRetention);
            }
            else
            {
                Vector3 retained = PlayerRigidBody.velocity * WallJumpVelocityRetention;
                retained.y = 0;
                PlayerRigidBody.velocity = movement + retained;
            }
        }
        else
        {
            Vector3 hVelocity = PlayerRigidBody.velocity;
            hVelocity.y = 0;
            float maxIncrease = Mathf.Max(SprintSpeed - Utility.ScalarProjection(hVelocity, movement), 0);
            PlayerRigidBody.velocity += Utility.MinByAbsolute(maxIncrease * movement.normalized, movement * Time.fixedDeltaTime);
        }
    }

    //TODO: Rip isCrouching out into a class scope variable?
    /// <summary>
    /// Move/Rotate camera based on current character status
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="isCrouching"></param>
    private void DoCameraEffects(float speed, bool isCrouching)
    {
        //Rotation
        if (IsWallRunning)
        {
            Vector3 tiltAxis = PlayerRigidBody.velocity;
            tiltAxis.y = 0;
            float angleSign = -Mathf.Sign(Vector3.SignedAngle(tiltAxis, WallNormal, Vector3.up));
            tiltAxis = transform.worldToLocalMatrix.MultiplyVector(tiltAxis);

            Quaternion targetRotation = Quaternion.AngleAxis(WallRunTiltDegrees * angleSign, tiltAxis);
            TiltObject.localRotation = Quaternion.RotateTowards(TiltObject.localRotation, targetRotation, TiltSpeed * Time.fixedDeltaTime);
        }
        else if (isCrouching && speed > 0)
        {
            Vector3 tiltAxis = Vector3.Cross(PlayerRigidBody.velocity, Vector3.up);
            tiltAxis.y = 0;
            tiltAxis = transform.worldToLocalMatrix.MultiplyVector(tiltAxis);

            Quaternion targetRotation = Quaternion.AngleAxis(WallRunTiltDegrees, tiltAxis);
            TiltObject.localRotation = Quaternion.RotateTowards(TiltObject.localRotation, targetRotation, TiltSpeed * Time.fixedDeltaTime);
        }
        else
        {
            TiltObject.localRotation = Quaternion.RotateTowards(TiltObject.localRotation, Quaternion.identity, TiltSpeed * Time.fixedDeltaTime);
        }

        //Translation
        float targetY = isCrouching && !IsWallRunning ? (CameraBasePosition.y - CrouchCameraDistance) - HeadCamera.localPosition.y : CameraBasePosition.y - HeadCamera.localPosition.y;
        HeadCamera.localPosition = HeadCamera.localPosition + new Vector3(0, Utility.MinByAbsolute(targetY, Mathf.Sign(targetY) * CrouchCameraSpeed * Time.fixedDeltaTime));
    }

    /// <summary>
    /// Determine viability of current wallrun, or start new wall run
    /// </summary>
    /// <param name="movement"></param>
    private void DoWallRunCheck(Vector3 movement)
    {
        WallRunForward = Vector3.ProjectOnPlane(HeadCamera.forward, WallNormal).normalized;
        WallRunUp = Vector3.Cross(WallRunForward, WallNormal) * Mathf.Sign(Vector3.SignedAngle(WallRunForward, WallNormal, Vector3.up));
        //Debug.DrawRay(transform.position, runVertical*10, Color.green, 10f);

        float inverseNormalMagnitude = Mathf.Max(Utility.ScalarProjection(PlayerRigidBody.velocity, -WallNormal), Utility.ScalarProjection(movement, -WallNormal));

        bool isBelowWallRunLimit = (WallRunCount < WallRunLimit || WallRunLimit == -1);
        //Velocity direction should be at least WallRunMinVelocityAngleFromDown degrees from the gravity direction
        bool isVelocityAngleOverMin = Mathf.Abs(Vector3.SignedAngle(PlayerRigidBody.velocity, Vector3.down, WallNormal)) > WallRunMinVelocityAngleFromDown;
        bool isNotSameAsLast = LastWallRunNormal != WallNormal || LastWallRunCollider != WallCollider;
        bool isSpaceHeldOrNotRequired = PlayerInput.Player.Jump.ReadValue<float>() > 0 || !SpaceToStartWallRun;

        bool isSpeedOverThreshold = inverseNormalMagnitude > 0.4;
        
        float WallAngle = Mathf.Sign(-Vector3.SignedAngle(WallRunForward, WallNormal, WallRunUp));
        movement.x *= WallAngle;
        bool isInputAngleAboveThreshold = Vector3.SignedAngle(movement, Vector3.forward, Vector3.up) > WallRunMaxInputAngle;

        if (!IsWallRunning && isBelowWallRunLimit && isSpeedOverThreshold && isSpaceHeldOrNotRequired && isNotSameAsLast && isVelocityAngleOverMin)
        {
            StartWallRun(WallRunForward, WallRunUp);
            Debug.Log("Starting run");
        }
        else if (IsWallRunning && (isInputAngleAboveThreshold || !isVelocityAngleOverMin))
        {
            EndWallRun();
            Debug.Log("Ending run");
        }
    }

    /// <summary>
    /// Wrapper for setting player physics friction
    /// </summary>
    /// <param name="val"></param>
    private void SetFriction(float val)
    {
        PlayerCollider.material.dynamicFriction = val;
        PlayerCollider.material.staticFriction = val;
    }

    /// <summary>
    /// Jump input action
    /// </summary>
    /// <param name="context"></param>
    private void UserJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        IsJumpingNextFrame = true;
    }

    /// <summary>
    /// Return the jump direction and force vector
    /// </summary>
    /// <param name="input"></param>
    /// <param name="suceeded"></param>
    /// <returns></returns>
    Vector3 GetJumpVector(Vector3 input, out bool suceeded)
    {
        if (IsGrounded)
        {
            TimeSinceLastJump = 0;
            suceeded = true;
            return new Vector3(input.x, JumpForce, input.z);
                
        }
        else if (IsWallRunning)
        {
            TimeSinceLastJump = 0;

            Vector3 result = WallJumpInLookDirection ? LookBasedWallJump() : AbsoluteWallJump();

            IsWallJumping = true;
            EndWallRun();
            suceeded = true;
            return result;
        }
        suceeded = false;
        return input;
    }

    /// <summary>
    /// Calculates a velocity direction with magnitude based on static values. Does not respond to look direction.
    /// </summary>
    /// <returns></returns>
    private Vector3 AbsoluteWallJump()
    {
        Vector3 horizontalVelocity = PlayerRigidBody.velocity;
        horizontalVelocity.y = 0;
        float forwardJumpForce = Mathf.Max(JumpForce - horizontalVelocity.magnitude, 0);
        Vector3 result = (transform.forward / JumpForce * forwardJumpForce + (WallNormal + Vector3.up)).normalized * JumpForce * WallJumpModifier;
        Debug.Log(result.magnitude);
        return result;
    }

    /// <summary>
    /// Calculates a velocity direction with magnitude based on a bezier curve and the current look direction.
    /// </summary>
    /// <returns></returns>
    private Vector3 LookBasedWallJump()
    {
        Quaternion toWallRunRotation = Quaternion.LookRotation(new Vector3(PlayerRigidBody.velocity.x, 0, PlayerRigidBody.velocity.z), Vector3.up);
        Quaternion fromWallRunRotation = Quaternion.Inverse(toWallRunRotation);

        Vector3 wallRunRelativeLook = fromWallRunRotation * HeadCamera.forward;
        float wallDirection = Mathf.Sign(-Vector3.SignedAngle(WallRunForward, WallNormal, WallRunUp));

        //Convert to spherical coordinates
        float rho = 1; //Range is 1 as it's a unit vector
        float azimuth = Mathf.Atan2(wallRunRelativeLook.x, wallRunRelativeLook.z); //Azimuth
        float altitude = Mathf.Asin(wallRunRelativeLook.y / rho); //Altitude

        //Clamp azimuth, and clamp altitute and remap to y force
        const float altitudeMin = -45 * Mathf.Deg2Rad;
        const float altitudeMax = 90 * Mathf.Deg2Rad;
        const float azimuthMin = -90 * Mathf.Deg2Rad;
        const float azimuthMax = -30 * Mathf.Deg2Rad;

        azimuth *= wallDirection;
        azimuth = Mathf.Clamp(azimuth, azimuthMin, azimuthMax);
        azimuth *= wallDirection;

        altitude = Mathf.Clamp(altitude, altitudeMin, altitudeMax);
        float t = Mathf.Clamp(Utility.Remap(altitude, altitudeMin, altitudeMax, 0, 1), 0, 1);
        altitude = WallJumpAngleCurve.Evaluate(t);
        altitude *= Mathf.Deg2Rad;

        //Convert back to cartesian and multiply
        Vector3 result = new Vector3(
            Mathf.Cos(altitude) * Mathf.Sin(azimuth),
            Mathf.Sin(altitude),
            Mathf.Cos(altitude) * Mathf.Cos(azimuth)
        );
        result = toWallRunRotation * result;
        result *= WallJumpModifier * JumpForce;
        //Debug.Log($"A: {azimuth}, E: {altitude} => {result}, Mag: {result.magnitude}, t: {t}");
        return result;
    }
}
