using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// A TextMeshProUGUI komponenshez adható szkript, amely a szöveget animálja.
/// A klasszikus "hullámzó" vagy "remegõ" effektet valósítja meg a betûkön.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AnimatedText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool isAnimating = false;

    [Header("Animációs Beállítások")]
    [Tooltip("Az animáció típusa.")]
    public AnimationType animationType = AnimationType.Wave;

    [Tooltip("Az animáció általános sebessége.")]
    [Range(0.1f, 20f)]
    public float speed = 5.0f;

    [Tooltip("A mozgás mértéke, azaz a kitérés nagysága.")]
    [Range(0.1f, 50f)]
    public float amplitude = 3.0f;

    [Tooltip("Csak Hullám animációnál: a hullámok sûrûsége.")]
    [Range(0.1f, 5f)]
    public float waveFrequency = 1.0f;

    public enum AnimationType
    {
        Wave,  // Hullámzó mozgás
        Jitter // Véletlenszerû remegés
    }

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // Amikor az objektum aktívvá válik, elindítjuk az animációt.
        StartAnimation();
    }

    void OnDisable()
    {
        // Amikor kikapcsolják, leállítjuk.
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
    /// Egy coroutine, ami képkockánként frissíti a szöveg karaktereinek pozícióját.
    /// </summary>
    private IEnumerator AnimateTextCoroutine()
    {
        // Ez a parancs elengedhetetlen, hogy hozzáférjünk a karakterek adataihoz.
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

            // A módosított vertex adatokat visszatöltjük a mesh-be.
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textMesh.textInfo.meshInfo[i].mesh.vertices = modifiedVertices[i];
                textMesh.UpdateGeometry(textMesh.textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }
}
