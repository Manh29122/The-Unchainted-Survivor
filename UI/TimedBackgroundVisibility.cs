using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedBackgroundVisibility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject backgroundObject;
    [SerializeField] private Canvas parentCanvas;

    [Header("Sequence")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float delayBeforeActivate = 2f;
    [SerializeField] private bool hideBackgroundAfterDelay = true;

    [Header("Targets")]
    [SerializeField] private GameObject[] objectsToKeepActive;
    [SerializeField] private GameObject[] objectsToActivateAfterDelay;

    private readonly List<GameObject> disabledCanvasObjects = new List<GameObject>();
    private Coroutine sequenceCoroutine;

    private void Reset()
    {
        backgroundObject = gameObject;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        if (backgroundObject == null)
        {
            backgroundObject = gameObject;
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnEnable()
    {
        if (!playOnEnable)
        {
            return;
        }

        PlaySequence();
    }

    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    public void PlaySequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine = StartCoroutine(PlaySequenceRoutine());
    }

    private IEnumerator PlaySequenceRoutine()
    {
        disabledCanvasObjects.Clear();

        if (backgroundObject != null)
        {
            backgroundObject.SetActive(true);
        }

        DeactivateOtherCanvasObjects();
        ActivateObjects(objectsToKeepActive, true);
        ActivateObjects(objectsToActivateAfterDelay, false);

        if (delayBeforeActivate > 0f)
        {
            yield return new WaitForSeconds(delayBeforeActivate);
        }

        ActivateObjects(objectsToActivateAfterDelay, true);

        if (hideBackgroundAfterDelay && backgroundObject != null)
        {
            backgroundObject.SetActive(false);
        }

        sequenceCoroutine = null;
    }

    private void DeactivateOtherCanvasObjects()
    {
        if (parentCanvas == null)
        {
            return;
        }

        Transform canvasTransform = parentCanvas.transform;
        for (int i = 0; i < canvasTransform.childCount; i++)
        {
            GameObject child = canvasTransform.GetChild(i).gameObject;

            if (child == backgroundObject || ContainsObject(objectsToKeepActive, child) || ContainsObject(objectsToActivateAfterDelay, child))
            {
                continue;
            }

            if (child.activeSelf)
            {
                child.SetActive(false);
                disabledCanvasObjects.Add(child);
            }
        }
    }

    private void ActivateObjects(GameObject[] targets, bool activeState)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(activeState);
            }
        }
    }

    private bool ContainsObject(GameObject[] targets, GameObject candidate)
    {
        if (targets == null || candidate == null)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    public void SetDelay(float value)
    {
        delayBeforeActivate = Mathf.Max(0f, value);
    }

    public void ShowBackgroundOnly()
    {
        if (backgroundObject != null)
        {
            backgroundObject.SetActive(true);
        }

        ActivateObjects(objectsToActivateAfterDelay, false);
    }

    public void ActivateTargetsNow()
    {
        ActivateObjects(objectsToActivateAfterDelay, true);

        if (hideBackgroundAfterDelay && backgroundObject != null)
        {
            backgroundObject.SetActive(false);
        }
    }
}
