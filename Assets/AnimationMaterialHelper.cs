using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimationMaterialHelper : MonoBehaviour
{
    [SerializeField]private AnimationMaterialDictionary _dictionary;

    // [SerializeField] private string ModelName;
    [SerializeField] private GameObject materialGameObject;

    private Renderer m_MyRenderer;
    private Renderer MyRenderer => m_MyRenderer != null ? m_MyRenderer : m_MyRenderer = materialGameObject.GetComponent<Renderer>();

    // Start is called before the first frame update
    void Start()
    {
        //no null ref, no null ref, stop!
        AnimationStarted(materialGameObject.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0).First().clip.name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AnimationStarted(string animationName)
    {
        Debug.Log($"{animationName} started!");
        var block = _dictionary.GetPropertyBlock(animationName);
        MyRenderer.SetPropertyBlock(block);
    }
}
