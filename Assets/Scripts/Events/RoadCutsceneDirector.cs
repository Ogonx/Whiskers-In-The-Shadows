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
    public BagManChase chasePatrol;
    public Animator bagManAnimator;

    [Header("Car")]
    public GameObject roadCar;
    public Transform carStartPoint;
    public Transform carEndPoint;
    public Transform bagManHitPoint;
    public Transform bagManChaseStart;
    public Transform carCrashPosition;
    public float carSpeed = 40f;
    public float bagManRunSpeed = 6f;

    [Header("Camera")]
    public Transform cinematicCamPoint;
    public float camMoveDuration = 1f;

    [Header("Cat")]
    public Transform catCrossTarget;
    public float catCrossSpeed = 4f;

    [Header("Audio")]
    public AudioSource windSource;
    public AudioSource chaseMusic;
    public AudioSource hornSource;
    public AudioSource crashSource;

    [Header("Effects")]
    public ParticleSystem smokeParticle;
    public ParticleSystem crashSmokeParticle;

    [Header("Trail")]
    public ScentTrail homeTrail;

    bool triggered = false;

    void Start()
    {
        if (roadCar) roadCar.SetActive(false);
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
        if (chasePatrol) chasePatrol.StopChase();
        if (chaseMusic) chaseMusic.Stop();

        catController.FreezeMovement();
        cameraFollow.frozen = true;

        if (windSource) windSource.volume = 0f;

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
            bagManTransform.position = bagManChaseStart.position;
            bagManTransform.rotation = bagManChaseStart.rotation;
        }

        if (bagManAnimator) bagManAnimator.SetBool("IsRunning", true);

        yield return StartCoroutine(MoveCamToCinematic());

        StartCoroutine(MoveBagManToHitPoint());

        yield return StartCoroutine(MoveCatAcrossRoad());

        yield return new WaitForSeconds(0.3f);

        if (hornSource) hornSource.Play();

        yield return new WaitForSeconds(0.5f);

        if (roadCar) roadCar.SetActive(true);
        roadCar.transform.position = carStartPoint.position;
        roadCar.transform.rotation = Quaternion.LookRotation((carEndPoint.position - carStartPoint.position).normalized);

        yield return StartCoroutine(DriveCarAcross());

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(MoveCamBackToCat());

        if (windSource)
        {
            windSource.Play();
            windSource.volume = 1f;
        }

        if (homeTrail) homeTrail.UnlockAndShow();

        cameraFollow.frozen = false;
        catController.UnfreezeMovement();

        gameObject.SetActive(false);
    }

    IEnumerator MoveBagManToHitPoint()
    {
        if (bagManTransform == null || bagManHitPoint == null) yield break;

        Rigidbody rb = bagManObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        while (Vector3.Distance(bagManTransform.position, bagManHitPoint.position) > 0.3f)
        {
            Vector3 dir = (bagManHitPoint.position - bagManTransform.position).normalized;
            bagManTransform.position += dir * bagManRunSpeed * Time.deltaTime;
            bagManTransform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }

        bagManTransform.position = bagManHitPoint.position;
        if (bagManAnimator) bagManAnimator.SetBool("IsRunning", false);
    }

    IEnumerator DriveCarAcross()
    {
        if (roadCar == null) yield break;

        bool hitBagMan = false;

        while (Vector3.Distance(roadCar.transform.position, carEndPoint.position) > 1f)
        {
            Vector3 dir = (carEndPoint.position - roadCar.transform.position).normalized;
            roadCar.transform.position += dir * carSpeed * Time.deltaTime;

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

                bagManObject.transform.SetParent(roadCar.transform);
                bagManObject.transform.localPosition = new Vector3(0f, 0f, 3.5f);
                bagManObject.transform.localRotation = Quaternion.identity;

                if (crashSource) crashSource.Play();

                if (smokeParticle)
                {
                    smokeParticle.gameObject.SetActive(true);
                    smokeParticle.transform.position = bagManHitPoint.position;
                    smokeParticle.Play();
                }
            }

            yield return null;
        }

        bagManObject.transform.SetParent(null);
        bagManObject.SetActive(false);

        if (carCrashPosition)
        {
            roadCar.transform.position = carCrashPosition.position;
            roadCar.transform.rotation = carCrashPosition.rotation;
        }

        if (crashSmokeParticle)
        {
            crashSmokeParticle.gameObject.SetActive(true);
            crashSmokeParticle.transform.position = carCrashPosition.position;
            crashSmokeParticle.Play();
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
            catController.transform.position += dir * catCrossSpeed * Time.deltaTime;
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
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        while (elapsed < camMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / camMoveDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, defaultFOV, t);
            yield return null;
        }
    }
}