using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public string zoneName = "zone1";
    private FirebaseZoneTracker tracker;

    private void Start()
    {
        tracker = FindObjectOfType<FirebaseZoneTracker>();
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            await tracker.SendZoneVisit(zoneName);
        }
    }
}