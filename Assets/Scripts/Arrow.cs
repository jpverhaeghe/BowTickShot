using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    // Constant values used in this script
    public const float DIST_TO_ARROW_PICKUP = 0.5f; // the min distance for a player to pick up the arrow to get it back
    public const int ARROW_LIFE_SPAN = 5;           // life span of an arrow in seconds before it is removed from game

    // Public variables used in this script
    public float drawForce = 0f;                    // force of the drawn bow to impact arrow flight
    public Vector3 initialPos;                      // storing the position of the arrow to help calculate score later

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Audio Clips for Arrow")]
    [SerializeField] AudioSource arrowPickup;

    // Private variables used in this script
    private GameManager gameManager;                // get a link to the game manager script so we can update arrows
    private GameObject playerArrowPickup;           // get a link to the player so we can check distances for arrow pick up
    private Rigidbody rb;                           // a rigid body for the arrow to apply force for movement and collisions
    private bool arrowPickedUp = false;

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
        // Using distance to player instead of collision as there are issues with capsule collider not picking up the arrow
        float distToPlayer = Mathf.Abs(Vector3.Distance(transform.position, playerArrowPickup.transform.position) );
        
        if ( (distToPlayer <= DIST_TO_ARROW_PICKUP) && !arrowPickedUp)
        {
            // add one here then immediately destroy the arrow (this offsets the removal when arrow is destroyed)
            gameManager.UpdateArrows(1);
            arrowPickedUp = true;

            // play a pickup sound
            arrowPickup.Play();

            // Note: this will cause a bug if the timing is just right when the invoked method is called and then this one.
            // Using invoke will stop this a bit and allow the sound effect to play
            Invoke("DestroyArrow", 0.5f);
        }

    } // end Update

    /// <summary>
    /// Destroys the arrow so it doesn't sit around on the screen too long
    /// </summary>
    private void DestroyArrow()
    {
        // if the player runs out of arrows, game is over
        if (gameManager.numArrows <= 0)
        {
            gameManager.EndGame();
        }

        Destroy(gameObject);

    } // end DestroyArrow

}
