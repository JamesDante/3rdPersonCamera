using UnityEngine;

public class PlayerInputController : MonoBehaviour {

    public PlayerInput Current;
    public Vector2 RightStickMultiplier = new Vector2(3, -1.5f);

    private bool fireInput = false;

	// Use this for initialization
	void Start () {
        Current = new PlayerInput();
	}

	// Update is called once per frame
	void Update () {
        
        // Retrieve our current WASD or Arrow Key _input
        // Using GetAxisRaw removes any kind of gravity or filtering being applied to the _input
        // Ensuring that we are getting either -1, 0 or 1
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector2 rightStickInput = new Vector2(Input.GetAxisRaw("RightH"), Input.GetAxisRaw("RightV"));

        // pass rightStick values in place of mouse when non-zero
        mouseInput.x = rightStickInput.x != 0 ? rightStickInput.x * RightStickMultiplier.x : mouseInput.x;
        mouseInput.y = rightStickInput.y != 0 ? rightStickInput.y * RightStickMultiplier.y : mouseInput.y;

        if (Input.GetButtonDown("Fire1")) 
        { 
            fireInput = true; 
        }
        else if(Input.GetButtonUp("Fire1"))
        {
            fireInput = false; 
        }

        bool jumpInput = Input.GetButtonDown("Jump");

        Current = new PlayerInput()
        {
            MoveInput = moveInput,
            MouseInput = mouseInput,
            JumpInput = jumpInput,
            FireInput = fireInput,
        };
	}
}

public struct PlayerInput
{
    public Vector3 MoveInput;
    public Vector2 MouseInput;
    public bool JumpInput;
    public bool RightMouseInput;
    public bool FireInput;
}
