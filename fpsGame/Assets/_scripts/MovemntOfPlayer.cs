using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovemntOfPlayer : MonoBehaviour
{
    //player components
    Rigidbody rb;
    CapsuleCollider pcc;

    //input holder
    Vector2 inputs;
    Vector2 mouseInput;//rotation


    //defaults
    Quaternion defaultRotation;

    //camera
    [Header("Camera")]
    public Transform eyeCam;
    public Transform eyeCamera;

    //positions on player
    [Header("Position On Players")]
    [SerializeField] Transform bottom;
    [SerializeField] Transform aboveBottom;
    [SerializeField] Transform center;
    [SerializeField] Transform aboveCenter;
    [SerializeField] Transform top;
    [SerializeField] Transform rightCenter;
    [SerializeField] Transform leftCenter;


    //Layer Masks
    public LayerMask groundLayer;

    
    //bools
    [Header("States")]
    [SerializeField] bool onGround;
    [SerializeField] bool inAir;
    [SerializeField] bool onSlopes;
    //[SerializeField] bool jumping;
   // [SerializeField] bool sprinting;


    [Header("Player Rotation")]
    public float xRotaionSens;
    public float yRotaionSens;
    public float maxYRotaionAngle;
    public float minYRotaionAngle;
    public float inairMultiplier;
    public float onGroundMultiplier;
    float rotationMultiplier;
    //[SerializeField] bool rotationSmoothing;
    //[SerializeField] float rotaionSmoothingValue;

    [Header("Basic Movement")]
    public float forwardSpeed=15f;
    public float backwardSpeedMultiplier=0.7f;
    public float forwardSpeedMultiplier=1f;
    public float sidewaysSpeed=10f;
    float currentSpeedMultiplier = 1f;
    public float maxSpeed=18f;
    [SerializeField]float groundDrag =2.4f;
    float currentDrag = 2f;
    Vector2 relaaiveVeloity;

    [Header("Air Movemnt ")]
    ForceMode airForceMode;
    public float triggerVelocity;
    public float airForcemultiplier;
    

    
    [Header("Sprinting")]
    [SerializeField]float sprintMultiplier;
    [SerializeField] bool sprint;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpInterval;
    [SerializeField] bool jumpPressed;
    [SerializeField] bool canjump;


    [Header("Ground Counter Movement")]
    public float groundCounterForceMultiplier;

    













    #region Unity Functions
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pcc = GetComponent<CapsuleCollider>();
        defaultRotation = eyeCam.transform.localRotation;
        currentSpeedMultiplier = forwardSpeedMultiplier;
        rotationMultiplier = onGroundMultiplier;

    }
    private void Update()
    {
        //inputs
        GetBasicPlayerInput();
        GetAdvancePlayerInput();
        CheckCollision();
        CalculateRelativeVelocity();

        //movemnts according to state
        if(onGround)
        {
            rotationMultiplier = onGroundMultiplier;
            //do running,jumping,crouching,and basic movement
            if ((inputs.x > 0 && rb.velocity.x > maxSpeed) || inputs.x < 0 && rb.velocity.x < -maxSpeed) inputs.x = 0f;
            if ((inputs.y > 0 && rb.velocity.y > maxSpeed) || inputs.y < 0 && rb.velocity.y < -maxSpeed) inputs.y = 0f;

            if (inputs.y > 0)
            {
                if (sprint)
                {
                   
                    currentSpeedMultiplier = sprintMultiplier;
                }
                else
                {
                    
                    currentSpeedMultiplier = forwardSpeedMultiplier;
                }
                
            } 
            else if (inputs.y < 0) currentSpeedMultiplier = backwardSpeedMultiplier;

            

            if(rb.velocity.magnitude>maxSpeed)
            {
                inputs = Vector2.zero;
                rb.velocity = rb.velocity.normalized * maxSpeed*0.9f;
                currentSpeedMultiplier = 0f;
            }

            //jumping
            jumpPressed = Input.GetButtonDown("Jump");
                
            

        }else if(inAir)
        {
            rotationMultiplier = inairMultiplier;
            //do air dash and rotation 
            if ((inputs.x > 0 && rb.velocity.x > maxSpeed) || inputs.x < 0 && rb.velocity.x < -maxSpeed) inputs.x = 0f;
            if ((inputs.y > 0 && rb.velocity.y > maxSpeed) || inputs.y < 0 && rb.velocity.y < -maxSpeed) inputs.y = 0f;

           

        }

        //common movement (rotation)
        PlayerRotation();


    }

    private void FixedUpdate()
    {
        //adding a contsant downward force
        rb.AddForce(Vector3.down * 1000f * Time.deltaTime);

        if(onGround)
        {
            if (canjump && jumpPressed)
            {
                PlayerJump();
            }

            //adjuct move sped according to input
            MoveThePlayer();
            CounterMoveThePlayer();


            

        }else if(inAir)
        {
            rb.drag = 0f;
            AirMovemnt();

        }

    }

  
    #endregion



    #region input Functions
    /// <summary>
    /// Getting mouse and basic movemnt input
    /// i.e left right forward back ward movemnt input
    /// </summary>
    void GetBasicPlayerInput()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        inputs = new Vector2(x, y).normalized ;
        mouseInput = new Vector2(mx, my);
    }


    /// <summary>
    /// advance input means 
    /// jumping ,crouching,wall running,dash,air dash
    /// </summary>
    void GetAdvancePlayerInput()
    {
        sprint = Input.GetKey(KeyCode.LeftShift);
    }
    #endregion

    #region player movemets

    #region Roatation 
    void PlayerRotation()
    {
        float xRot = mouseInput.x * Time.deltaTime * xRotaionSens*rotationMultiplier;
        float yRot = mouseInput.y * Time.deltaTime * yRotaionSens*rotationMultiplier;
        
        
            //rotating along y axis i.e side ways movement
            // we are rotaing the player body
            //calculating the angle according to the input and then applying it to player
            Quaternion xAdjust = Quaternion.AngleAxis(xRot, Vector3.up);
            transform.localRotation *= xAdjust;


            //rotating along x axis  i.e looking up and down 
            //rotating the camera

            //ccculating by how much to rotate
            Quaternion yAdjust = Quaternion.AngleAxis(yRot, Vector3.left);
            //adding the cams rotaion to the rotaion which we calculate above
            Quaternion yDelata = eyeCam.localRotation * yAdjust;
            //finding the diffrence between the sum and the default identity zero rotation
            float yDelatAngle = Quaternion.Angle(defaultRotation, yDelata);
            // Debug.Log(yDelatAngle);
            //if the angle is less than the max rotation limit then setting the rotation
            if (yDelatAngle < maxYRotaionAngle)
            {
                eyeCam.localRotation = yDelata;
            }
        

               
        
       
    }
    #endregion

    #region basic movement

    void MoveThePlayer()
    {
        float forwardForce = forwardSpeed * inputs.y * currentSpeedMultiplier;
        rb.AddForce(transform.forward * forwardForce);

        float sidewayForce =  sidewaysSpeed * inputs.x;
        rb.AddForce(transform.right * sidewayForce);
    }

    void CounterMoveThePlayer()
    {
        if((inputs.x==0 && Mathf.Abs(relaaiveVeloity.x) > 0f )||(inputs.x>0 && relaaiveVeloity.x<0)||(inputs.x<0 && relaaiveVeloity.x>0 ))
        {
            rb.AddForce(transform.right * -relaaiveVeloity.x * groundCounterForceMultiplier);
        }
        if ((inputs.y == 0 && Mathf.Abs(relaaiveVeloity.y)> 0f) || (inputs.y > 0 && relaaiveVeloity.y < 0) || (inputs.y < 0 && relaaiveVeloity.y > 0))
        {
            rb.AddForce(transform.forward * -relaaiveVeloity.y * groundCounterForceMultiplier);
        }
    }

    private void AirMovemnt()
    {
        if (new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > maxSpeed-2f)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, rb.velocity / 2f, 8f * Time.deltaTime);
            inputs = Vector2.zero;
        }

        if (rb.velocity.magnitude < triggerVelocity && inputs.magnitude > 0)
        {
            airForceMode = ForceMode.Impulse;

        }
        else
        {
            airForceMode = ForceMode.Force;
        }
       //rb.AddForce(transform.forward * maxSpeed * inputs.y*airForcemultiplier, airForceMode);
    //s    rb.AddForce(transform.right * maxSpeed * inputs.x*airForcemultiplier, airForceMode);
    }

    #endregion


    #region AdvanceMovemnt
    void PlayerJump()
    {
        rb.drag = 1f;
        rb.AddForce(Vector3.up * jumpForce * Time.deltaTime, ForceMode.Impulse);
        canjump = false;
        if (rb.velocity.y < 0f) rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (rb.velocity.y > 0f)
        {
            rb.velocity = rb.velocity / 2f;//Vector3.Lerp(rb.velocity, rb.velocity / 2f, 4 * Time.deltaTime);
        }
        Invoke("ResetJump", jumpInterval);

    }

    void ResetJump()
    {
        canjump = true;
    }
    #endregion

    #endregion


    #region Calculations
    void CalculateRelativeVelocity()
    {
        Vector3 velo = transform.InverseTransformDirection(new Vector3(rb.velocity.x, 0f, rb.velocity.z));
        relaaiveVeloity = new Vector2(velo.x, velo.z);

        
       

    }
    #endregion
    #region Collision Detection

    void CheckCollision()                
    {
        
        if(Physics.CheckSphere(aboveBottom.position - new Vector3(0f, 0.15f, 0f), pcc.radius - 0.1f,groundLayer))
        {
          //  Debug.Log("Gtouch");
            onGround = true;
            currentDrag = groundDrag;
            rb.drag = currentDrag;
        }else
        {
            onGround = false;
            rb.drag = 0.1f;
        }
    }

    #endregion


    #region debug Draw 

    private void OnDrawGizmos()
    {
       

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aboveBottom.position - new Vector3(0f, 0.15f, 0f), pcc.radius - 0.1f);
    }

    #endregion
}
