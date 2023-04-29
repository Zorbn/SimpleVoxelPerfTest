using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float Speed = 10f;
    private const float JumpForce = 10f;
    private const float Sensitivity = 2f;

    public static readonly Vector3 Size = new(1, 2, 1);
    
    private Transform _camera;
    private Vector3 _cameraRotation;
    private Transform _transform;
    private VoxelWorld _voxelWorld;
    private float _yVelocity;
    private bool _isGrounded;
    
    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _voxelWorld = GameObject.Find("World").GetComponent<VoxelWorld>();
        
        _transform = transform;

        _camera = Camera.main!.transform;
        _camera.parent = _transform;
        _camera.localPosition = new Vector3(0f, Size.y * 0.25f, 0f);
    }

    private void Move()
    {
        var inputDirection = (Input.GetAxisRaw("Horizontal") * transform.right + Input.GetAxisRaw("Vertical") * transform.forward).normalized;
        var position = _transform.position;
        var nextPosition = position;
        
        nextPosition.x += Speed * Time.deltaTime * inputDirection.x;
        if (VoxelPhysics.HasBlockCollision(_voxelWorld, nextPosition, Size))
        {
            nextPosition.x = position.x;
        }
        
        nextPosition.z += Speed * Time.deltaTime * inputDirection.z;
        if (VoxelPhysics.HasBlockCollision(_voxelWorld, nextPosition, Size))
        {
            nextPosition.z = position.z;
        }

        _yVelocity -= VoxelPhysics.Gravity * Time.deltaTime;
        
        if (_isGrounded && Input.GetButton("Jump"))
        {
            _yVelocity = JumpForce;
        }
        
        nextPosition.y += _yVelocity * Time.deltaTime;
        _isGrounded = false;
        if (VoxelPhysics.HasBlockCollision(_voxelWorld, nextPosition, Size))
        {
            nextPosition.y = position.y;

            if (_yVelocity < 0f)
            {
                // The player hit the ground.
                _isGrounded = true;
            }
                
            _yVelocity = 0f;
        }
        
        _transform.position = nextPosition;
    }

    private void Look()
    {
        _cameraRotation.y += Input.GetAxis("Mouse X") * Sensitivity;
        _transform.rotation = Quaternion.Euler(0f, _cameraRotation.y, 0f);
        _cameraRotation.x -= Input.GetAxis("Mouse Y") * Sensitivity;
        _cameraRotation.x = Mathf.Clamp(_cameraRotation.x, -89f, 89f);
        _camera.localRotation = Quaternion.Euler(_cameraRotation.x, 0f, 0f);
    }

    private void Update()
    {
        Move();
        Look();
    }
}