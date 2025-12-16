using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeModelLoaders.UI
{
    /// <summary>
    /// Zeigt kurze Textmeldungen auf einem World-Space-Canvas an und blendet sie
    /// nach einer definierten Zeit wieder aus.
    /// </summary>
    public class ToastPresenter : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 2.5f;
        [SerializeField] private float fadeSpeed = 2f;

        private Coroutine? _currentRoutine;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            messageText = GetComponentInChildren<Text>();
        }

        public void ShowMessage(string message)
        {
            if (messageText == null || canvasGroup == null)
                return;

            messageText.text = message;
            if (_currentRoutine != null)
                StopCoroutine(_currentRoutine);

            _currentRoutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);

            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }

            canvasGroup.gameObject.SetActive(false);
            _currentRoutine = null;
        }
    }
}
