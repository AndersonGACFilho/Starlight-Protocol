using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// A class which manages pages of UI elements
/// and the game's UI
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Page Management")]
    [Tooltip("The pages (Panels) managed by the UI Manager")]
    public List<UIPage> pages;
    [Tooltip("The index of the active page in the UI")]
    public int currentPage = 0;
    [Tooltip("The page (by index) switched to when the UI Manager starts up")]
    public int defaultPage = 0;

    [Header("Pause Settings")]
    [Tooltip("The index of the pause page in the pages list")]
    public int pausePageIndex = 1;
    [Tooltip("Whether or not to allow pausing")]
    public bool allowPause = true;

    [Header("Input Actions & Controls")]
    [Tooltip("Input action used to toggle the pause menu.")]
    public InputAction pauseAction;

    // Whether or not the application is paused
    private bool isPaused = false;

    // A list of all UI element classes
    private List<UIelement> UIelements;

    // The event system handling UI navigation
    [HideInInspector]
    public EventSystem eventSystem;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    private void OnEnable()
    {
        pauseAction.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    private void OnDisable()
    {
        pauseAction.Disable();
    }

    /// <summary>
    /// Description:
    /// Finds and stores all UIElements in the UIElements list
    /// Input:
    /// None
    /// Return:
    /// void (no return)
    /// </summary>
    private void SetUpUIElements()
    {
        UIelements = FindObjectsByType<UIelement>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
    }

    /// <summary>
    /// Description:
    /// Gets the event system from the scene if one exists
    /// If one does not exist a warning will be displayed
    /// Input:
    /// None
    /// Return:
    /// void (no return)
    /// </summary>
    private void SetUpEventSystem()
    {
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("There is no event system in the scene but you are trying to use the UIManager. \n" +
                "All UI in Unity requires an Event System to run. \n" + 
                "You can add one by right clicking in hierarchy then selecting UI->EventSystem.");
        }
    }

    /// <summary>
    /// Description:
    /// If the game is paused, unpauses the game.
    /// If the game is not paused, pauses the game.
    /// Inputs:
    /// None
    /// Retuns:
    /// void (no return)
    /// </summary>
    public void TogglePause()
    {
        if (allowPause)
        {
            if (isPaused)
            {
                SetOverlayPagesActive(false);
                SetHudPagesActive(true);
                Time.timeScale = 1;
                isPaused = false;
            }
            else
            {
                GoToPage(pausePageIndex);
                Time.timeScale = 0;
                isPaused = true;
            }
        }      
    }

    /// <summary>
    /// Description:
    /// Goes through all UI elements and calls their UpdateUI function
    /// Input:
    /// None
    /// Return:
    /// void (no return)
    /// </summary>
    public void UpdateUI()
    {
        SetUpUIElements();
        foreach (UIelement uiElement in UIelements)
        {
            uiElement.UpdateUI();
        }
    }

    /// <summary>
    /// Standard Unity function called once when the script instance first exists in runtime of the game. Called before Start.
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Description:
    /// Default Unity function that runs once when the script is first started and before Update
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Start()
    {
        SetUpEventSystem();
        SetUpPages();
        SetUpUIElements();
        GoToPage(defaultPage);
        UpdateUI();
    }

    /// <summary>
    /// Description:
    /// Default function from Unity that runs every frame
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Update()
    {
        CheckPauseInput();
    }

    /// <summary>
    /// Description:
    /// If the input manager is set up, reads the pause input
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void CheckPauseInput()
    {
        if (pauseAction.triggered)
        {
            TogglePause();
        }
    }
    /// <summary>
    /// Description:
    /// Goes to a page by that page's index
    /// Inputs: 
    /// int page
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="pageIndex">The index in the page list to go to</param>
    public void GoToPage(int pageIndex)
    {
        SetUpPages();

        if (pageIndex >= 0 && pageIndex < pages.Count && pages[pageIndex] == null)
        {
            SetOverlayPagesActive(false);
            SetHudPagesActive(true);
            currentPage = pageIndex;
            UpdateUI();
            return;
        }

        if (pageIndex >= 0 && pageIndex < pages.Count && pages[pageIndex] != null)
        {
            SetOverlayPagesActive(false);
            SetHudPagesActive(true);
            pages[pageIndex].gameObject.SetActive(true);
            pages[pageIndex].SetSelectedUIToDefault();
            currentPage = pageIndex;
            UpdateUI();
            return;
        }

        Debug.LogWarning($"{nameof(UIManager)}: Page index {pageIndex} is not configured.");
    }

    public bool TryGoToPage(int pageIndex)
    {
        SetUpPages();

        if (pageIndex >= 0 && pageIndex < pages.Count && pages[pageIndex] == null)
        {
            SetOverlayPagesActive(false);
            SetHudPagesActive(true);
            currentPage = pageIndex;
            UpdateUI();
            return true;
        }

        if (pageIndex >= 0 && pageIndex < pages.Count && pages[pageIndex] != null)
        {
            SetOverlayPagesActive(false);
            SetHudPagesActive(true);
            pages[pageIndex].gameObject.SetActive(true);
            pages[pageIndex].SetSelectedUIToDefault();
            currentPage = pageIndex;
            UpdateUI();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Description:
    /// Goes to a page by that page's name
    /// Inputs: 
    /// string pageName
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="pageName">The name of the page in the game you want to go to, if their are duplicates this picks the first found</param>
    public void GoToPageByName(string pageName)
    {
        SetUpPages();
        UIPage page = pages.Find(item => item.name == pageName);
        int pageIndex = pages.IndexOf(page);
        GoToPage(pageIndex);
    }

    public bool TryGoToPageByName(string pageName)
    {
        SetUpPages();
        UIPage page = pages.Find(item => item && item.name.Contains(pageName));
        if (!page)
        {
            return false;
        }

        int pageIndex = pages.IndexOf(page);
        return TryGoToPage(pageIndex);
    }

    /// <summary>
    /// Description:
    /// Turns all stored pages on or off depending on parameters
    /// Input: 
    /// bool enable
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="activated">The true or false value to set all page game object's activeness to</param>
    public void SetActiveAllPages(bool activated)
    {
        SetUpPages();

        if (pages != null)
        {
            foreach (UIPage page in pages)
            {
                if (page == null)
                {
                    continue;
                }

                if (!activated && IsHudPage(page))
                {
                    page.gameObject.SetActive(true);
                    continue;
                }

                page.gameObject.SetActive(activated);
            }
        }
    }

    private void SetOverlayPagesActive(bool activated)
    {
        SetUpPages();

        if (pages == null)
        {
            return;
        }

        foreach (UIPage page in pages)
        {
            if (page == null || IsHudPage(page))
            {
                continue;
            }

            page.gameObject.SetActive(activated);
        }
    }

    private void SetHudPagesActive(bool activated)
    {
        SetUpPages();

        if (pages == null)
        {
            return;
        }

        foreach (UIPage page in pages)
        {
            if (page != null && IsHudPage(page))
            {
                page.gameObject.SetActive(activated);
            }
        }
    }

    private void SetUpPages()
    {
        if (pages != null && pages.Count > 0 && pages.Any(page => page != null))
        {
            return;
        }

        pages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(page => GetPageSortOrder(page.name))
            .ThenBy(page => page.transform.GetSiblingIndex())
            .ToList();
    }

    private int GetPageSortOrder(string pageName)
    {
        if (pageName.Contains("BaseUI"))
        {
            return 0;
        }

        if (pageName.Contains("GameOver"))
        {
            return 1;
        }

        if (pageName.Contains("Victory"))
        {
            return 2;
        }

        if (pageName.Contains("Pause"))
        {
            return 3;
        }

        return 10;
    }

    private bool IsHudPage(UIPage page)
    {
        if (page == null)
        {
            return false;
        }

        return page.name.Contains("BaseUI")
            || page.GetComponentInChildren<ScoreDisplay>(true) != null
            || page.GetComponentInChildren<PowerUpDisplay>(true) != null
            || page.GetComponentInChildren<WaveDisplay>(true) != null;
    }
}
