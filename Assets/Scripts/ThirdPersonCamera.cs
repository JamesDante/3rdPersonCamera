using UnityEngine;

[RequireComponent(typeof(ThirdPersonCameraCollisionHandler))]
public class ThirdPersonCamera : MonoBehaviour
{
    public float Height = 2.0f;
    public float Distance = 5.0f;
    public GameObject PlayerTarget;
    public ThirdPersonCameraCollisionHandler collision;

    private SuperCharacterController _controller;
    private PlayerInputController _input;
    private Transform _target;
    private PlayerMachine _machine;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 _destination = Vector3.zero;
    private Vector3 _camraVelocity = Vector3.zero;
    private Color _matColor;
    private float _yRotation;
    private float _commonAdjustmentDistance = 3;
    private float _targetDistance = 3;
    private float _aVel;
    private float _dist;
    private float _preTargetHeight = -1;
    private float _targetHeightAdjustmentDistance = -1;
    private bool _noMoveCamera = false;

    void Awake()
    {
        collision = GetComponent<ThirdPersonCameraCollisionHandler>();
        _input = PlayerTarget.GetComponent<PlayerInputController>();
        _machine = PlayerTarget.GetComponent<PlayerMachine>();
        _controller = PlayerTarget.GetComponent<SuperCharacterController>();
        _target = PlayerTarget.transform;
        _targetPosition = _target.position;
        _targetPosition.y = _targetPosition.y + Height;

        _targetHeightAdjustmentDistance = _targetPosition.y;
        _preTargetHeight = _targetHeightAdjustmentDistance;

        collision.Initialize(Camera.main);
    }

    void Update()
    {
        _targetPosition = _target.position;
        _targetPosition.y = _targetPosition.y + Height;

        if (Mathf.Abs(_preTargetHeight - _targetPosition.y) > 3.3f ||
            _controller.currentGround.IsGrounded(false, 0) ||
            transform.position.y < _target.position.y * 0.8f)
        {
            _targetHeightAdjustmentDistance = _targetPosition.y;
        }

        _preTargetHeight = Mathf.Lerp(_preTargetHeight, _targetHeightAdjustmentDistance, 2 * Time.deltaTime);

        _targetPosition.y = _preTargetHeight;

        _yRotation += _input.Current.MouseInput.y;

        if (_yRotation > 90) _yRotation = 90;
        else if (_yRotation < -90) _yRotation = -90;

        Vector3 left = Vector3.Cross(_machine.lookDirection, _controller.up);
        transform.rotation = Quaternion.LookRotation(_machine.lookDirection, _controller.up);
        transform.rotation = Quaternion.AngleAxis(_yRotation, left) * transform.rotation;

        _destination = _targetPosition;
        _destination -= transform.forward * _commonAdjustmentDistance;

        transform.position = _destination;

        SMoveToTarget(true);

        var fadeDistance = Vector3.Distance(_targetPosition, transform.position) * 0.5f;
        ToggleFadeTarget(fadeDistance);
    }

    //Turn target into transparent when too close.
    private void ToggleFadeTarget(float fade)
    {
        if (fade < 0.35) fade = 0;

        SkinnedMeshRenderer[] skinnedRenderers = _target.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skin in skinnedRenderers)
        {
            foreach (Material m in skin.materials)
            {
                if (fade < 1) SetMaterialBlendMode(m, "Fade");
                else SetMaterialBlendMode(m, "Opaque");

                _matColor.a = fade;
                m.color = _matColor;
            }
        }

        MeshRenderer[] renderers = _target.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mesh in renderers)
        {
            foreach (Material m in mesh.materials)
            {
                if (fade < 1) SetMaterialBlendMode(m, "Fade");
                else SetMaterialBlendMode(m, "Opaque");

                _matColor = m.color;
                _matColor.a = fade;
                m.color = _matColor;
            }
        }
    }

    private void SMoveToTarget(bool useCollision)
    {
        _targetDistance = Distance;
        _dist = 15;

        var dis = collision.HandleCollisionZoomDistance(_destination, _targetPosition, 0.18f, Distance);
        if (dis != Mathf.Infinity && dis < Distance)
        {
            _targetDistance = dis;

            _destination = _targetPosition;

            _dist = Mathf.Abs(dis - _commonAdjustmentDistance);
            if (dis < _commonAdjustmentDistance)
            {
                _destination -= transform.forward * dis;
            }
            else
            {
                _destination -= transform.forward * _commonAdjustmentDistance;
            }
        }

        _commonAdjustmentDistance = Mathf.SmoothDamp(_commonAdjustmentDistance, _targetDistance, ref _aVel, _dist * Time.deltaTime);

        transform.position = _destination;
    }

    private void SetMaterialBlendMode(Material material, string BLEND_MODE)
    {
        switch (BLEND_MODE)
        {
            case "Opaque":
                material.SetFloat("_Mode", 0);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case "Fade":
                material.SetFloat("_Mode", 2); //set to fade mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }
}
