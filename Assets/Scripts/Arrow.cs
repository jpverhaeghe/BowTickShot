using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    // Constant values used in this script
    public const float DIST_TO_ARROW_PICKUP = 0.5f; // the min distance for a player to pick up the arrow to get it back
    public const float CURVE_TIME = 0.5f;           // time to cure the arrow, making it constant
    public const int ARROW_LIFE_SPAN = 5;           // life span of an arrow in seconds before it is removed from game

    // Public variables used in this script
    public float drawForce = 0f;                    // force of the drawn bow to impact arrow flight
    public float drawTorque = 0f;                   // the torque given to the 
    public Vector3 initialPos;                      // storing the position of the arrow to help calculate score later

    // Serialized variables to show in the Unity editor but not public to other scripts

    // Private variables used in this script
    private GameManager gameManager;                // get a link to the game manager script so we can update arrows
    private GameObject playerArrowPickup;           // get a link to the player so we can check distances for arrow pick up
    private Rigidbody rb;                           // a rigid body for the arrow to apply force for movement and collisions
    private float curveTimer;                       // a timer for the curve of the arrow

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // get the rigid body for this arrow
        rb = GetComponent<Rigidbody>();

        // store the initial position of the arrow to give bonuses to score
        initialPos = transform.position;

        // When it is instantiated, apply the given force to the arrow in the direction it is facing 
        rb.AddForce(transform.forward * drawForce, ForceMode.Impulse);

        // and to the right if there is torque (if it is zero it won't go anywyhere)
        rb.AddForce(transform.right * drawTorque, ForceMode.Impulse);

        // reverse the direction and set up the timer
        drawTorque = -drawTorque;
        curveTimer = CURVE_TIME;

        // testing adding a torque to see if I can get a curve (may need to make this a timed update - removing torque after a bit)
        // this didn't not work as I expected, so adding force in fixed update. 
        //rb.AddTorque(transform.forward * drawTorque, ForceMode.Impulse);

        // set up the arrow to destroy itself
        Invoke("DestroyArrow", ARROW_LIFE_SPAN);

        // get the game manager script from the GameManager object that is in the scene
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        // get a link to the player object to check distance'
        playerArrowPickup = GameObject.Find("ArrowPickup");

    } // end Start

    /// <summary>
    /// Use Update to update impulse force on the arrow if a curve was there
    /// </summary>
    void Update()
    {
        // only apply curve to the arrow if there is a torque
        if (drawTorque != 0)
        {
            // remove a bit from the timer
            curveTimer -= Time.deltaTime;

            // if the timer is up change direction
            if (curveTimer < 0)
            {
                // Add impulse force for returning now
                rb.AddForce(transform.right * drawTorque, ForceMode.Impulse);
                drawTorque = 0;
            }
        }

        // to fix issues with collision (player collision capsule doesn't hit the ground except at one point) so use distance
        float distToPlayer = Mathf.Abs(Vector3.Distance(transform.position, playerArrowPickup.transform.position) );
        
        if (distToPlayer <= DIST_TO_ARROW_PICKUP)
        {
            // add one here then immediately destroy the arrow (this offsets the removal when arrow is destroyed)
            gameManager.UpdateArrows(1);
            // Note: this may cause a bug if the timing is just right when the invoked method is called and then this one.
            DestroyArrow();
        }

    } // end Update

    /// <summary>
    /// Destroys the arrow so it doesn't sit around on the screen too long
    /// </summary>
    private void DestroyArrow()
    {
        Destroy(gameObject);

        // remove one from the arrows
        gameManager.UpdateArrows(-1);

    } // end DestroyArrow

}
