using UnityEngine;
using System.Collections;

/*
 * Example implementation of the SuperStateMachine and SuperCharacterController
 */
[RequireComponent(typeof(SuperCharacterController))]
[RequireComponent(typeof(PlayerInputController))]
public class PlayerMachine : SuperStateMachine {

    public float turnSmoothing = 20f;
    private Quaternion targetRotation;

    public Transform AnimatedMesh;
    public float WalkSpeed = 6.0f;
    public float WalkAcceleration = 30.0f;
    public float JumpAcceleration = 5.0f;
    public float JumpHeight = 3.0f;
    public float Gravity = 25.0f;
    public float JumpControl = 3f;

    // Add more states by comma separating them
    enum PlayerStates { Idle, Walk, Jump, DoubleJump, Fall, WallSlide, Climb, Climbing }

    private SuperCharacterController controller;

    // current velocity
    private Vector3 moveDirection;
    // current direction our character's art is facing
    public Vector3 lookDirection { get; private set; }

    private PlayerInputController input;

    private Vector3 collisionVector;
    private Vector3 climbingPoint;
    private int jumpCount;

	void Start () {
	    // Put any code here you want to run ONCE, when the object is initialized

        input = gameObject.GetComponent<PlayerInputController>();

        // Grab the controller object from our object
        controller = gameObject.GetComponent<SuperCharacterController>();
		
		// Our character's current facing direction, planar to the ground
        lookDirection = transform.forward;
        targetRotation = transform.rotation;

        // Set our currentState to idle on startup
        currentState = PlayerStates.Idle;
	}

    protected override void EarlyGlobalSuperUpdate()
    {
		// Rotate out facing direction horizontally based on mouse _input
        // (Taking into account that this method may be called multiple times per frame)
        lookDirection = Quaternion.AngleAxis(input.Current.MouseInput.x * (controller.deltaTime / Time.deltaTime), controller.up) * lookDirection;
        //if (_input.Current.FireInput) 
        //{
        //    lookDirection = Vector3.Lerp(lookDirection, transform.forward, turnSmoothing * Time.deltaTime);
        //    //lookDirection = Quaternion.AngleAxis(_input.Current.MouseInput.x * (controller.deltaTime / Time.deltaTime), controller.up) * lookDirection;
        //}

        if (moveDirection.x != 0 || moveDirection.z != 0)
        {
            targetRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z).normalized);
        }
        targetRotation = Quaternion.Lerp(AnimatedMesh.rotation, targetRotation, turnSmoothing * Time.deltaTime);
        // Put any code in here you want to run BEFORE the state's update function.
        // This is run regardless of what state you're in

        Debug.Log(currentState);
    }

    protected override void LateGlobalSuperUpdate()
    {
        // Put any code in here you want to run AFTER the state's update function.
        // This is run regardless of what state you're in

        //Debug.Log("end" + moveDirection + ":" + currentState);

        // Move the player by our velocity every frame
        transform.position += moveDirection * controller.deltaTime;

        // Rotate our mesh to face where we are "looking"
        if (moveDirection != Vector3.zero && !input.Current.FireInput)
        {
            AnimatedMesh.rotation = Quaternion.LookRotation(lookDirection, controller.up);
            AnimatedMesh.rotation = targetRotation;
        }
        else if (input.Current.FireInput)
        {
            AnimatedMesh.rotation = Quaternion.LookRotation(lookDirection, controller.up);
        }
    }

    private bool AcquiringGround()
    {
        return controller.currentGround.IsGrounded(false, 0.01f);
    }

    private bool MaintainingGround()
    {
        return controller.currentGround.IsGrounded(true, 0.5f) ;
    }

    public void RotateGravity(Vector3 up)
    {
        lookDirection = Quaternion.FromToRotation(transform.up, up) * lookDirection;
    }

    /// <summary>
    /// Constructs a vector representing our movement local to our lookDirection, which is
    /// controlled by the camera
    /// </summary>
    private Vector3 LocalMovement()
    {
        Vector3 right = Vector3.Cross(controller.up, lookDirection);

        Vector3 local = Vector3.zero;

        if (input.Current.MoveInput.x != 0)
        {
            local += right * input.Current.MoveInput.x;
        }

        if (input.Current.MoveInput.z != 0)
        {
            local += lookDirection * input.Current.MoveInput.z;
        }

        return local.normalized;
    }

    // Calculate the initial velocity of a jump based off gravity and desired maximum height attained
    private float CalculateJumpSpeed(float jumpHeight, float gravity)
    {
        return Mathf.Sqrt(2 * jumpHeight * gravity);
    }

    private bool IsOnEdge()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position + transform.forward * 0.8f + transform.up * 2.2f, -transform.up, out hit, .5f, controller.Walkable))
        {
            var nu = Vector3.Dot(hit.normal, controller.up);
            if (nu > 0.5) 
            {
                climbingPoint = hit.point;
                return true;
            }
        }

        return false;

        //RaycastHit LHit;
        //RaycastHit RHit;

        //Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(-0.32f, 1.2f, 0.5f)), transform.TransformDirection(new Vector3(0f, -0.5f, 0.0f)), out LHit, 1f,  controller.Walkable);
        //Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0.32f, 1.2f, 0.5f)), transform.TransformDirection(new Vector3(0f, -0.5f, 0.0f)), out RHit, 1f, controller.Walkable);

        //if (LHit.transform
        //    || RHit.transform)
        //{
        //    RaycastHit hit;
        //    Physics.Raycast(transform.position + transform.forward * 0.8f + transform.up * 2.2f, -transform.up, out hit, .5f, controller.Walkable);

        //    var nu = Vector3.Dot(hit.normal, controller.up);
        //    Debug.Log(nu);
        //    if (nu > 0)
        //    {
        //        climbingPoint = hit.point;
        //        return true;
        //    }
        //}

        //return false;
    }

	/*void Update () {
	 * Update is normally run once on every frame update. We won't be using it
     * in this case, since the SuperCharacterController component sends a callback Update 
     * called SuperUpdate. SuperUpdate is recieved by the SuperStateMachine, and then fires
     * further callbacks depending on the state
	}*/

    // Below are the three state functions. Each one is called based on the name of the state,
    // so when currentState = Idle, we call Idle_EnterState. If currentState = Jump, we call
    // Jump_SuperUpdate()
    void Idle_EnterState()
    {
        controller.EnableSlopeLimit();
        controller.EnableClamping();

        jumpCount = 0;
    }

    void Idle_SuperUpdate()
    {
        // Run every frame we are in the idle state

        if (input.Current.JumpInput)
        {
            currentState = PlayerStates.Jump;
            return;
        }

        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            currentState = PlayerStates.Walk;
            return;
        }

        // Apply friction to slow us to a halt
        moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 10.0f * controller.deltaTime);
    }

    void Idle_ExitState()
    {
        // Run once when we exit the idle state
    }

    void Walk_SuperUpdate()
    {
        if (input.Current.JumpInput)
        {
            currentState = PlayerStates.Jump;
            return;
        }

        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            moveDirection = Vector3.MoveTowards(moveDirection, LocalMovement() * WalkSpeed, WalkAcceleration * controller.deltaTime);
        }
        else
        {
            currentState = PlayerStates.Idle;
            return;
        }
    }

    void DoubleJump_EnterState()
    {
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;

        jumpCount = 2;
        moveDirection -= verticalMoveDirection;
        moveDirection += controller.up * CalculateJumpSpeed(JumpHeight, Gravity);
    }

    void DoubleJump_SuperUpdate()
    {
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;
        planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, LocalMovement() * WalkSpeed, JumpAcceleration * JumpControl * controller.deltaTime);

        if (Vector3.Angle(verticalMoveDirection, controller.up) > 90 && AcquiringGround())
        {
            moveDirection = planarMoveDirection;
            currentState = PlayerStates.Idle;
            return;
        }

        collisionVector = controller.IsOnTheWall(LocalMovement());
        if (collisionVector != Vector3.zero)
        {
            currentState = PlayerStates.WallSlide;
            return;
        }

        foreach (var c in controller.collisionData)
        {
            if (c.normal == -controller.up)
            {
                verticalMoveDirection.y = 0;
                break;
            }
        }

        if (moveDirection.y == 0)
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (IsOnEdge())
        {
            currentState = PlayerStates.Climb;
            return;
        }

        verticalMoveDirection -= controller.up * Gravity * controller.deltaTime;
        moveDirection = planarMoveDirection + verticalMoveDirection;
    }

    void Jump_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();

        jumpCount = 1;
        moveDirection += controller.up * CalculateJumpSpeed(JumpHeight, Gravity);
    }

    void Jump_SuperUpdate()
    {       
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;
        planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, LocalMovement() * WalkSpeed, JumpAcceleration * JumpControl * controller.deltaTime);

        if (Vector3.Angle(verticalMoveDirection, controller.up) > 90 && AcquiringGround())
        {
            moveDirection = planarMoveDirection;
            currentState = PlayerStates.Idle;
            return;            
        }

        foreach(var c in controller.collisionData)
        {
            if (c.normal == -controller.up)
            {
                verticalMoveDirection.y = 0;
                break;
            }
        }

        if (moveDirection.y <= 0) 
        {
            currentState = PlayerStates.Fall;
            return;            
        }

        if (input.Current.JumpInput)
        {
            currentState = PlayerStates.DoubleJump;
            return;
        }

        collisionVector = controller.IsOnTheWall(LocalMovement());
        if (collisionVector != Vector3.zero)
        {
            currentState = PlayerStates.WallSlide;
            return;
        }

        if (IsOnEdge()) 
        {
            currentState = PlayerStates.Climb;
            return;
        }

        verticalMoveDirection -= controller.up * Gravity * controller.deltaTime;
        moveDirection = planarMoveDirection + verticalMoveDirection;
    }

    void WallSlide_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();

        moveDirection = Vector3.zero;
        jumpCount = 0;
    }

    void WallSlide_SuperUpdate()
    {
        if (input.Current.MoveInput == Vector3.zero || Vector3.Dot(collisionVector, LocalMovement()) > 0)
        {
            currentState = PlayerStates.Fall;
            return;
        }

        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;

        if (Vector3.Angle(verticalMoveDirection, controller.up) > 90 && AcquiringGround())
        {
            moveDirection = planarMoveDirection;
            currentState = PlayerStates.Idle;
            return;
        }

        if (input.Current.JumpInput)
        {
            moveDirection = collisionVector * 8;
            currentState = PlayerStates.Jump;
            return;
        }

        moveDirection -= controller.up * Gravity * 0.1f * Time.deltaTime;
    }

    void WallSlide_ExitState()
    {
        collisionVector = Vector3.zero;
    }

    void Climb_EnterState()
    {
        transform.position = transform.position + new Vector3(0, climbingPoint.y - 1.2f - transform.position.y, 0);
        moveDirection = Vector3.zero;
    }

    void Climb_SuperUpdate()
    {
        if (input.Current.JumpInput)
        {
            currentState = PlayerStates.Climbing;
            return;
        }
    }

    void Climbing_SuperUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, climbingPoint, WalkSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, climbingPoint) < 0.3f) 
        {
            currentState = PlayerStates.Idle;
            return;
        }
    }

    void Fall_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();
    }

    void Fall_SuperUpdate()
    {
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;
        planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, LocalMovement() * WalkSpeed, JumpAcceleration * JumpControl * controller.deltaTime);

        if (AcquiringGround())
        {
            moveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
            currentState = PlayerStates.Idle;
            return;
        }

        if (input.Current.JumpInput && jumpCount < 2)
        {
            currentState = PlayerStates.DoubleJump;
            return;
        }

        if (controller.IsOnTheWall(LocalMovement()) != Vector3.zero)
        {
            currentState = PlayerStates.WallSlide;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            var tempDir = Vector3.MoveTowards(moveDirection, LocalMovement() * WalkSpeed, WalkAcceleration * controller.deltaTime);
            moveDirection.x = tempDir.x;
            moveDirection.z = tempDir.z;
        }

        if (IsOnEdge())
        {
            currentState = PlayerStates.Climb;
            return;
        }

        verticalMoveDirection -= controller.up * Gravity * 1.4f * controller.deltaTime;
        moveDirection = planarMoveDirection + verticalMoveDirection;
    }

    void OnDrawGizmos()
    {
        //Left Hand IK Visual Ray
        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(-0.3f, 1.8f, 0.5f)), transform.TransformDirection(new Vector3(0.3f, -1.2f, 0.0f)), Color.green);

        //Right Hand IK Visual Ray
        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0.3f, 1.8f, 0.5f)), transform.TransformDirection(new Vector3(-0.3f, -1.2f, 0.0f)), Color.green);

        var dir = transform.up + transform.forward / 2;

        Debug.DrawRay(transform.position + transform.forward*0.8f + transform.up * 2.2f, -transform.up, Color.red);
    }
}
