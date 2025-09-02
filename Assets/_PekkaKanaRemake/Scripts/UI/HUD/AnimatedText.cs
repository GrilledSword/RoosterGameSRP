using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// A TextMeshProUGUI komponenshez adhat� szkript, amely a sz�veget anim�lja.
/// A klasszikus "hull�mz�" vagy "remeg�" effektet val�s�tja meg a bet�k�n.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AnimatedText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool isAnimating = false;

    [Header("Anim�ci�s Be�ll�t�sok")]
    [Tooltip("Az anim�ci� t�pusa.")]
    public AnimationType animationType = AnimationType.Wave;

    [Tooltip("Az anim�ci� �ltal�nos sebess�ge.")]
    [Range(0.1f, 20f)]
    public float speed = 5.0f;

    [Tooltip("A mozg�s m�rt�ke, azaz a kit�r�s nagys�ga.")]
    [Range(0.1f, 50f)]
    public float amplitude = 3.0f;

    [Tooltip("Csak Hull�m anim�ci�n�l: a hull�mok s�r�s�ge.")]
    [Range(0.1f, 5f)]
    public float waveFrequency = 1.0f;

    public enum AnimationType
    {
        Wave,  // Hull�mz� mozg�s
        Jitter // V�letlenszer� remeg�s
    }

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // Amikor az objektum akt�vv� v�lik, elind�tjuk az anim�ci�t.
        StartAnimation();
    }

    void OnDisable()
    {
        // Amikor kikapcsolj�k, le�ll�tjuk.
        StopAnimation();
    }

    public void StartAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;
        StartCoroutine(AnimateTextCoroutine());
    }

    public void StopAnimation()
    {
        isAnimating = false;
        StopAllCoroutines();
    }

    /// <summary>
    /// Egy coroutine, ami k�pkock�nk�nt friss�ti a sz�veg karaktereinek poz�ci�j�t.
    /// </summary>
    private IEnumerator AnimateTextCoroutine()
    {
        // Ez a parancs elengedhetetlen, hogy hozz�f�rj�nk a karakterek adataihoz.
        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;
        Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
        Vector3[][] modifiedVertices = new Vector3[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();
            modifiedVertices[i] = new Vector3[originalVertices[i].Length];
        }

        while (isAnimating)
        {
            if (textMesh.textInfo.characterCount == 0)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3 offset = Vector3.zero;
                switch (animationType)
                {
                    case AnimationType.Wave:
                        offset = new Vector3(0, Mathf.Sin(Time.time * speed + i * waveFrequency * 0.1f) * amplitude, 0);
                        break;
                    case AnimationType.Jitter:
                        offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * amplitude * 0.1f;
                        break;
                }

                for (int j = 0; j < 4; j++)
                {
                    modifiedVertices[materialIndex][vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + offset;
                }
            }

            // A m�dos�tott vertex adatokat visszat�ltj�k a mesh-be.
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textMesh.textInfo.meshInfo[i].mesh.vertices = modifiedVertices[i];
                textMesh.UpdateGeometry(textMesh.textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }
}
