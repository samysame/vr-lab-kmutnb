using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialPanelManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("UI")]
    public TMP_Text titleText;
    public Image tutorialImage;
    public TMP_Text pageText;

    [Header("Buttons")]
    public Button previousButton;
    public Button nextButton;
    public Button closeButton;

    private Sprite[] currentPages;
    private int currentPageIndex = 0;

    private void Start()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        ClosePanel();
    }

    public void OpenTutorial(string tutorialTitle, Sprite[] pages)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("Aucune image assignée pour ce tutoriel : " + tutorialTitle);
            return;
        }

        currentPages = pages;
        currentPageIndex = 0;

        if (titleText != null)
            titleText.text = tutorialTitle;

        if (panel != null)
            panel.SetActive(true);

        UpdatePage();
    }

    public void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void NextPage()
    {
        if (currentPages == null || currentPages.Length == 0)
            return;

        if (currentPageIndex < currentPages.Length - 1)
        {
            currentPageIndex++;
            UpdatePage();
        }
    }

    public void PreviousPage()
    {
        if (currentPages == null || currentPages.Length == 0)
            return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        if (tutorialImage != null)
        {
            tutorialImage.sprite = currentPages[currentPageIndex];
            tutorialImage.preserveAspect = true;
        }

        if (pageText != null)
            pageText.text = (currentPageIndex + 1) + " / " + currentPages.Length;

        if (previousButton != null)
            previousButton.interactable = currentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentPageIndex < currentPages.Length - 1;
    }
}