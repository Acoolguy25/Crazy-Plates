using UnityEngine;
using System.Collections;

public class DebugCharacter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator NotStart()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second before toggling again
        bool ragdollActive = true;
        for (int i  = 0; i < 4; i++)
        {
            SendMessage("SetRagdoll", ragdollActive, SendMessageOptions.DontRequireReceiver);
            ragdollActive = !ragdollActive; // Toggle the ragdoll state
            yield return new WaitForSeconds(2f); // Wait for 1 second before toggling again
        }
    }
}
