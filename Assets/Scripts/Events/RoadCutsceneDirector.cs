using UnityEngine;
using System.Collections;

public class RoadCutsceneDirector : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform bagManTransform;
    public GameObject bagManObject;
    public BagManChase chasePatrol;    // stopped at the start of this sequence
    public Animator bagManAnimator;

    [Header("Car")]
    public GameObject roadCar;           // the car GameObject, hidden until the cutscene
    public Transform carStartPoint;      // where the car spawns
    public Transform carEndPoint;        // where the car drives to
    public Transform bagManHitPoint;     // where BagMan stands waiting to get hit
    public Transform bagManChaseStart;   // where BagMan is repositioned for the cutscene
    public Transform carCrashPosition;   // final position of the car after the hit
    public float carSpeed = 40f;         // how fast the car drives across
    public float bagManRunSpeed = 6f;    // how fast BagMan runs to the hit point

    [Header("Camera")]
    public Transform cinematicCamPoint;  // fixed camera position for the cutscene
    public float camMoveDuration = 1f;   // how long the camera takes to move to cinematic position

    [Header("Cat")]
    public Transform catCrossTarget;     // where the cat walks to during the cutscene
    public float catCrossSpeed = 4f;

    [Header("Audio")]
    public AudioSource windSource;
    public AudioSource chaseMusic;  // stopped at the start
    public AudioSource hornSource;  // car horn played before the car appears
    public AudioSource crashSource; // crash sound when car hits BagMan

    [Header("Effects")]
    public ParticleSystem smokeParticle;       // smoke at the hit point
    public ParticleSystem crashSmokeParticle;  // smoke at the crash position

    [Header("Trail")]
    public ScentTrail homeTrail; // activated after the cutscene to guide player home

    bool triggered = false;

    void Start()
    {
        if (roadCar) roadCar.SetActive(false); // hide car at start
        if (smokeParticle) smokeParticle.gameObject.SetActive(false);
        if (crashSmokeParticle) crashSmokeParticle.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        if (chasePatrol) chasePatrol.StopChase(); // stop BagMan patrol
        if (chaseMusic) chaseMusic.Stop();        // stop chase music

        catController.FreezeMovement();
        cameraFollow.frozen = true;

        if (windSource) windSource.volume = 0f; // silence wind

        if (bagManObject && bagManChaseStart)
        {
            bagManObject.SetActive(true);
            Rigidbody rb = bagManObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            bagManTransform.position = bagManChaseStart.position; // reposition BagMan for cutscene
            bagManTransform.rotation = bagManChaseStart.rotation;
        }

        if (bagManAnimator) bagManAnimator.SetBool("IsRunning", true); // start run animation

        yield return StartCoroutine(MoveCamToCinematic()); // move camera to fixed position

        StartCoroutine(MoveBagManToHitPoint()); // BagMan starts running toward hit point

        yield return StartCoroutine(MoveCatAcrossRoad()); // cat crosses the road automatically

        yield return new WaitForSeconds(0.3f);

        if (hornSource) hornSource.Play(); // play car horn

        yield return new WaitForSeconds(0.5f);

        // activate and position the car
        if (roadCar) roadCar.SetActive(true);
        roadCar.transform.position = carStartPoint.position;
        roadCar.transform.rotation = Quaternion.LookRotation((carEndPoint.position - carStartPoint.position).normalized);

        yield return StartCoroutine(DriveCarAcross()); // drive car across and hit BagMan

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(MoveCamBackToCat()); // pan camera back to cat

        if (windSource)
        {
            windSource.Play();
            windSource.volume = 1f; // restore wind
        }

        if (homeTrail) homeTrail.UnlockAndShow(); // show trail home

        cameraFollow.frozen = false;
        catController.UnfreezeMovement(); // give control back

        gameObject.SetActive(false);
    }

    IEnumerator MoveBagManToHitPoint()
    {
        if (bagManTransform == null || bagManHitPoint == null) yield break;

        Rigidbody rb = bagManObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // run BagMan toward the hit point
        while (Vector3.Distance(bagManTransform.position, bagManHitPoint.position) > 0.3f)
        {
            Vector3 dir = (bagManHitPoint.position - bagManTransform.position).normalized;
            bagManTransform.position += dir * bagManRunSpeed * Time.deltaTime;
            bagManTransform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }

        bagManTransform.position = bagManHitPoint.position;
        if (bagManAnimator) bagManAnimator.SetBool("IsRunning", false); // stop running when he arrives
    }

    IEnumerator DriveCarAcross()
    {
        if (roadCar == null) yield break;

        bool hitBagMan = false;

        while (Vector3.Distance(roadCar.transform.position, carEndPoint.position) > 1f)
        {
            Vector3 dir = (carEndPoint.position - roadCar.transform.position).normalized;
            roadCar.transform.position += dir * carSpeed * Time.deltaTime; // drive car forward

            // check if car is close enough to hit BagMan
            if (!hitBagMan && bagManHitPoint != null &&
                Vector3.Distance(roadCar.transform.position, bagManHitPoint.position) < 5f)
            {
                hitBagMan = true;

                Rigidbody rb = bagManObject.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                bagManObject.transform.SetParent(roadCar.transform); // attach BagMan to car
                bagManObject.transform.localPosition = new Vector3(0f, 0f, 3.5f);
                bagManObject.transform.localRotation = Quaternion.identity;

                if (crashSource) crashSource.Play(); // play crash sound

                if (smokeParticle)
                {
                    smokeParticle.gameObject.SetActive(true);
                    smokeParticle.transform.position = bagManHitPoint.position;
                    smokeParticle.Play(); // play impact smoke
                }
            }

            yield return null;
        }

        bagManObject.transform.SetParent(null); // detach BagMan from car
        bagManObject.SetActive(false); // hide BagMan

        if (carCrashPosition)
        {
            roadCar.transform.position = carCrashPosition.position;
            roadCar.transform.rotation = carCrashPosition.rotation;
        }

        if (crashSmokeParticle)
        {
            crashSmokeParticle.gameObject.SetActive(true);
            crashSmokeParticle.transform.position = carCrashPosition.position;
            crashSmokeParticle.Play(); // play crash smoke at final position
        }
    }

    IEnumerator MoveCamToCinematic()
    {
        if (cinematicCamPoint == null) yield break;
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (elapsed < camMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / camMoveDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, cinematicCamPoint.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, cinematicCamPoint.rotation, t);
            yield return null;
        }

        mainCamera.transform.position = cinematicCamPoint.position;
        mainCamera.transform.rotation = cinematicCamPoint.rotation;
    }

    IEnumerator MoveCatAcrossRoad()
    {
        if (catCrossTarget == null) yield break;

        Vector3 targetPos = new Vector3(catCrossTarget.position.x, catController.transform.position.y, catCrossTarget.position.z);

        while (Vector3.Distance(catController.transform.position, targetPos) > 0.3f)
        {
            Vector3 dir = (targetPos - catController.transform.position).normalized;
            catController.transform.position += dir * catCrossSpeed * Time.deltaTime; // move cat across road
            catController.transform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }

        catController.transform.position = targetPos;
    }

    IEnumerator MoveCamBackToCat()
    {
        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation;
        float defaultFOV = 60f;

        Vector3 catEyeLevel = catController.transform.position + Vector3.up * 0.5f;
        Vector3 dir = catEyeLevel - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized); // face toward the cat

        while (elapsed < camMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / camMoveDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // pan back
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, defaultFOV, t); // restore FOV
            yield return null;
        }
    }
}