using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [Header("타이틀 UI")]
    [SerializeField]
    private GameObject titleUI;

    [Header("Options UI")]
    [SerializeField]
    private GameObject optionUI;

    [Header("Options Button")]
    [SerializeField]
    private Button optionButton;

    [Header("Return Button")]
    [SerializeField]
    private Button returnButton;

    private void Awake()
    {
        if (titleUI == null || optionUI == null || optionButton == null)
        {
            Debug.LogError("타이틀 UI 또는 옵션 UI가 할당되지 않았습니다. UI를 설정해주세요.");
            return;
        }

        // Options 버튼 클릭 이벤트 등록
        optionButton.onClick.AddListener(OnClickOptionsButton);
        // Return 버튼 클릭 이벤트 등록
        returnButton.onClick.AddListener(OnClickReturnButton);

        // 초기 타이틀 UI 활성화
        titleUI.SetActive(true);
        optionUI.SetActive(false);
    }

    private void OnClickOptionsButton()
    {
        /* SoundManager.instance가 null이 아닐 때만 사운드 재생 
        if (SoundManager.instance != null)
            SoundManager.instance.PlayClickSound();
            */
        // 타이틀 UI 비활성화, 옵션 UI 활성화
        titleUI.SetActive(false);
        optionUI.SetActive(true);

        // Return 버튼 활성화
        returnButton.gameObject.SetActive(true);
        optionButton.gameObject.SetActive(false);
    }

    private void OnClickReturnButton()
    {
        /* SoundManager.instance가 null이 아닐 때만 사운드 재생 
        if (SoundManager.instance != null)
            SoundManager.instance.PlayClickSound();
            */
        // 옵션 UI 비활성화, 타이틀 UI 활성화
        optionUI.SetActive(false);
        titleUI.SetActive(true);

        // Return 버튼 비활성화, Options 버튼 활성화
        returnButton.gameObject.SetActive(false);
        optionButton.gameObject.SetActive(true);
    }
}