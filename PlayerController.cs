using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class NewBehaviourScript : MonoBehaviour
{
    //스피드 조정 변수
    [SerializeField]    //private로 설정했을때 인스펙터 창에 보이지 않게 된다 public은 보임 데이터 직렬화인데 이진화로 만드는 것
    private float walkSpeed;    //이 스크립트 내에서만 제어할 수 있게 함 유니티프로그램안에서는 변경 불가능

    [SerializeField]
    private float runSpeed;     //walkSpeed와 runSpeed만으로는 움직임을 제어하는 함수가 두개로 나누어져야한다.
    [SerializeField]
    private float crouchSpeed;  //앉기 변수

    private float applySpeed;   //하지만 applySpeed에 walkSpeed와 runSpeed를 대입하면 끝이다.

    [SerializeField]
    private float jumpForce;    //플레이어 점프 변수

    //상태 변수
    private bool isRun = false;     //걷는지 뛰는지 구별하는 함수
    private bool isCrouch = false;  //앉기
    private bool isGround = true;   //땅에서만 점프를 할 수 있게 설정

    [SerializeField]
    private float crouchPosY;
    private float originPosY;
    private float applyCrouchPosY;  //crouchPosY와 originPosY를 applyCrouchPosY값에 넣는다.


    //땅 착지 여부
    private CapsuleCollider capsuleCollider;

    //민감도
    [SerializeField]    //시리얼라이즈필드를 만들고 인스펙터창에서 스크립트로 드래그해서 해주는것도 가능 (유니티에서는 권장하지 않음)
    private float lookSensitivity;  //카메라 민감도 (자신에 맞게 조정)

    //카메라 한계
    [SerializeField]
    private float cameraRotationLimit;  //설정을 안하면 고개를 돌릴때 360도가 돌아가서 이상해진다.
    private float currentCameraRotationX = 0;   //정면을 바라보게 0으로 설정


    [SerializeField]
    private Camera theCamera;

    private Rigidbody myRigid;  //플레이어 몸, 이것을 설정하지 않으면 본체는 겉모습만 보여줄뿐 만지게 되면 통과하게 된다. Collider로 충돌 영역 설정 

    // Start is called before the first frame update
    void Start()
    {
        //theCamera = FindObjectOfType<Camera>(); //카메라가 한개가 아니라 여러개가 있을 수 있으므로 이 방법은 사용하지 않는다.
        capsuleCollider = GetComponent<CapsuleCollider>();  //플레이어가 땅에 접촉해 있는CapsuleCollider를 capsuleCollider변수에 넣음
        myRigid = GetComponent<Rigidbody>();    //myRigid변수에 Rigidbody를 넣는다. (시리얼라이즈필드를 생성하는 것보다 빠름)
        applySpeed = walkSpeed;

        //초기화
        originPosY = theCamera.transform.localPosition.y;   //월드기준이 아닌 플레이어 기준의 위치이기 때문에 상대적인 변수인 local을 사용
        applyCrouchPosY = originPosY;   //기본 서있는 상태
    }
    
    // Update is called once per frame (프레임마다 호출되는 함수)
    void Update()
    {
        IsGround();
        TryJump();
        TryRun();   //Move함수 전에 넣어서 뛰는지 걷는지 판단한다.
        TryCrouch();
        Move();
        CameraRotation();
        CharacterRotation();
    }

    private void TryCrouch()    //앉기 시도
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))  //컨트롤 키 누르면 앉기를 입력
        {
            Crouch();
        }
    }

    private void Crouch()   //앉기 또는 서기에 대한 내용
    {
        isCrouch = !isCrouch;

        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }
        StartCoroutine(CrouchCoroutine());
    }
    
    IEnumerator CrouchCoroutine()   //부드러운 앉기 동작
    {
        float _posY = theCamera.transform.localPosition.y;
        int count = 0;

        while (_posY != applyCrouchPosY)
        {
            count++;
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.3f);    //보간을 사용하는 함수(Mathf.Lerp). 처음엔 빠르게 증가했다가 목적지에 도달할수록 천천히 줄어들게 된다. //0.1f로 앉기 속도제어
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            if (count > 15)
                break;
            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
    }
    private void IsGround()
    {
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);  //Raycast는 어느 거리만큼 광선을 쏘는것이다. 아래방향으로 쏘게 하여 땅에 붙어있는지 확인. 0.1f를 붙이는 이유는
    }

    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            Jump();
        }
    }

    private void Jump()
    {
        //앉은 상태 해제
        if (isCrouch)
        {
            Crouch();
        }
        myRigid.velocity = transform.up * jumpForce;    //velocity는 myRigid가 현재 어느방향으로 움직이는 속도 이다.
    }

    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Running();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            RunningCancel();
        }
    }
    private void Running()   //달리기
    {
        if (isCrouch)
            Crouch();

        isRun = true;
        applySpeed = runSpeed;
    }
    private void RunningCancel()   //달리기 취소
    {
        isRun = false;
        applySpeed = walkSpeed;
    }


    private void Move()
    {
        float _moveDirX = Input.GetAxisRaw("Horizontal");   //input 입력,좌우 ad를 눌렀을때 1이나 -1를 리턴을 해서 _moveDirX변수에 리턴된다.
        float _moveDirZ = Input.GetAxisRaw("Vertical"); //input 입력,상하 wd를 눌렀을때 1이나 -1를 리턴

        Vector3 _moveHorizontal = transform.right * _moveDirX;   //transform은 기본 컴포넌트가 갖고있는 위치값 회전값에 right를 사용하겠다는 것이다. Vector3 값에 (1,0,0)기본값으로 들어가 있음
        Vector3 _moveVertical = transform.forward * _moveDirZ;

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed;
        //normalized 하는 이유
        //(1,0,0) + (0,0,1)
        //(1,0,1) = 2
        //(0.5, 0, 0.5) = 1     합이 1이 나오도록 정규화 시켜주면 1초에 얼마나 이동시킬건지 개발 입장에서도 계산이 편해진다.

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);  //transform.position(현재위치), 업데이트 내장함수가 60프레임에 1초이기 때문에 1초동안 _velocity만큼 움직이게 한다. Time.deltaTime = 0.016의 값이다.
    }

    private void CharacterRotation()    //좌우 캐릭터 회전
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_characterRotationY));
    }
    private void CameraRotation()   //위아래 캐릭터 회전
    {
        float _xRotation = Input.GetAxisRaw("Mouse Y");     //마우스는 2차원 공간이기때문에 x와 y 뿐이다. 유니티는 3차원 공간이기 때문에 X이다 위아래
        float _cameraRotationX = _xRotation * lookSensitivity;
        currentCameraRotationX -= _cameraRotationX;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);    //45도로 currentCameraRotationX값이 -45도 +45도 값으로 고정이 된다.

        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);   //localEulerAngles는 transform의 rotation이라고 보면 된다
        
    }
}