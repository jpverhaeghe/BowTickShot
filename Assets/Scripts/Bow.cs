using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Referencing the following website: Creating a Bow and Arrow experience for VR: Part 1 (immersive-insiders.com) - by Ashray Pai
// Most code is original based on the site as the site is for a VR bow with pulling back using touch controllers
// I've added the string in a different way (still using a line renderer - thanks Alan!) and adjusted pull strength based on time
public class Bow : MonoBehaviour
{
    // Constant values used in this script

    // Public variables used in this script

    // Serialized variables to show in the Unity editor but not public to other scripts
    [Header("Bow String Animation Objects")]
    [SerializeField] LineRenderer bowString;        // the line that represents the string to show pull back
    [SerializeField] AnimationCurve stringCurve;    // the string draw curve to make it feel more natural
    [SerializeField] AnimationCurve releaseCurve;   // the string release curve to make it feel more natural
    [SerializeField] Transform bowStringTop;        // the starting point of the bow string to be able to get back there
    [SerializeField] Transform bowStringBottom;     // the ending point of the bow string to be able to show pull
    [SerializeField] Transform bowStringStart;      // the starting point of the bow string to be able to get back there
    [SerializeField] Transform bowStringEnd;        // the ending point of the bow string to be able to show pull
    [SerializeField] float maxStringDrawTime = 2;   // Need to have a top level on the draw time

    [Header("Arrow objects for arrow animation")]
    [SerializeField] GameObject arrowInBow;         // the game object that stays in the bow until fired
    [SerializeField] GameObject arrowProjectile;    // the game object that gets fired when the player releases the mouse
    [SerializeField] Transform arrowInBowStart;     // the starting point of the arrow in the bow
    [SerializeField] Transform arrowInBowEnd;       // the ending point of the arrow when string is pulled back
    [SerializeField] int reloadTime = 1;            // time in seconds before reloading

    [Header("Game Information")]
    [SerializeField] GameManager gameManager;

    [Header("Audio Clips for Bow")]
    [SerializeField] AudioSource bowShooting;

    // Private variables used in this script
    private float stringDrawTime = 0f;              // the draw time that will be used as a multiplier for force on the arrow
    private float drawTimeDivisor = 1.5f;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // make sure the string is set up correctly at the top and bottom (Not necessary as it is being done in update)
        bowString.SetPosition(0, bowStringTop.position);
        bowString.SetPosition(1, bowStringStart.position);
        bowString.SetPosition(2, bowStringBottom.position);

    } // end Start

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        if (gameManager.gameInProgress)
        {
            ShootArrow();
        }
        
    } // end Update

    /// <summary>
    /// Shoots an arrow out of the bow and sets up reload time if the string was released
    /// </summary>
    private void ShootArrow()
    {
        // make sure the string is set up correctly at the top and bottom
        bowString.SetPosition(0, bowStringTop.position);
        bowString.SetPosition(2, bowStringBottom.position);

        // check to see if the player has released the left mouse button and there is an arrow loaded
        if (Input.GetMouseButtonUp(0) && arrowInBow.activeSelf)
        {
            // remove the arrow from the bow
            arrowInBow.SetActive(false);

            // shoot the arrow with a force based on the string draw time (using a prefab)
            Arrow arrowFired = Instantiate(arrowProjectile, arrowInBow.transform.position, arrowInBow.transform.rotation).GetComponent<Arrow>();
            bowShooting.Play();

            if (stringDrawTime < maxStringDrawTime)
            {
                arrowFired.drawForce = (stringDrawTime / drawTimeDivisor);
            }
            else
            {
                arrowFired.drawForce = (maxStringDrawTime / drawTimeDivisor);
            }

            // remove the arrow from the quiver and add one to the number of arrows in the world
            gameManager.UpdateArrows(-1);
            gameManager.numArrowsFired++;

            // reload the arrow after a time
            Invoke("ReloadArrow", reloadTime);

            // reset the draw time to the minimum
            stringDrawTime = 0;

            // reset the string back to the starting point (not working, not sure yet why)
            // TODO: Add a curve here as well with LERP to make it feel like it is reverberating!
            bowString.SetPosition(1, bowStringStart.position);

        }
        // if the player is holding down the mouse button, keep track of how long (will use as a force scalar in some way)
        else if (Input.GetMouseButton(0) && arrowInBow.activeSelf)
        {
            // get the draw time so far
            stringDrawTime += Time.deltaTime;

            // animate the draw of the string by moving the middle point of the bow from the starting position by the draw time
            // calculate the pull strength so far, using the time for draw divided by the maximum seconds of draw so we don't go over 1

            // Helped here by Alan Zucconi
            // stringDrawTime: [0, maxStringDrawTime]
            // pullValue:       [0, 1]
            float pullValue = Mathf.Clamp01(stringDrawTime / maxStringDrawTime);
            Vector3 pullVector = Vector3.Lerp(bowStringStart.position, bowStringEnd.position, stringCurve.Evaluate(pullValue));

            bowString.SetPosition(1, pullVector);

            // move the arrow in the bow back based on the string position
            Vector3 arrowInBowVector = Vector3.Lerp(arrowInBowStart.position, arrowInBowEnd.position, stringCurve.Evaluate(pullValue));
            arrowInBow.transform.position = arrowInBowVector;
        }
        // otherwise the bow is not being held in a draw state or fire (not needed as above if triggers on release of button and resets it)
        else
        {
            bowString.SetPosition(1, bowStringStart.position);
            arrowInBow.transform.position = arrowInBowStart.position;
        }

    }

    /// <summary>
    /// Waits a bit before putting the arrow back in the bow
    /// </summary>
    private void ReloadArrow()
    {
        // if there is an arrow in the quiver, reload the arrow in the bow
        if (gameManager.numArrows > 0)
        {
            arrowInBow.transform.position = arrowInBowStart.position;
            arrowInBow.SetActive(true);
        }
        // otherwise start a timer to check again in a bit (in case the player picks another arrow up)
        else
        {
            Invoke("ReloadArrow", reloadTime);
        }

    } // end ReloadArrow
}
