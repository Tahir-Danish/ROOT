using UnityEngine;

public enum AppState
{
    HOME,
    SCAN,
    ANALYZING,
    IDENTIFIED,
    UNCERTAIN,
    UNKNOWN,
    ERROR,
    EXPLORE,
    PLANT_DETAILS
}

public class AppNavigationShell : MonoBehaviour
{
    public AppState currentState = AppState.HOME;
    
    public PlantDetailsUIController plantDetailsController;
    public GameObject homeScreen;
    public GameObject scanScreen;
    public GameObject analyzingScreen;
    public GameObject identifiedScreen;
    public GameObject uncertainScreen;
    public GameObject unknownScreen;
    public GameObject errorScreen;
    public GameObject exploreScreen;
    public ARSpeciesCard arSpeciesCard; 

    private DetectionResult _currentDetection;

    void Start()
    {
        if (plantDetailsController != null)
            plantDetailsController.Initialize(this);
            
        TransitionTo(AppState.HOME);
    }

    public void StartScan()
    {
        TransitionTo(AppState.SCAN);
    }

    public void SimulateDetection(DetectionResult result)
    {
        _currentDetection = result;
        TransitionTo(AppState.ANALYZING);
        Invoke(nameof(ResolveDetection), 1f);
    }

    private void ResolveDetection()
    {
        if (_currentDetection.status == "identified")
        {
            TransitionTo(AppState.IDENTIFIED);
            if (arSpeciesCard != null)
            {
                arSpeciesCard.PopulateData(_currentDetection.species_id);
            }
        }
        else if (_currentDetection.status == "uncertain")
        {
            TransitionTo(AppState.UNCERTAIN);
        }
        else if (_currentDetection.status == "unknown")
        {
            TransitionTo(AppState.UNKNOWN);
        }
        else
        {
            TransitionTo(AppState.ERROR);
        }
    }

    public void GoToExplore()
    {
        TransitionTo(AppState.EXPLORE);
    }

    public void GoToPlantDetails()
    {
        TransitionTo(AppState.PLANT_DETAILS);
        if (plantDetailsController != null)
        {
            plantDetailsController.OpenDetails(_currentDetection.species_id);
        }
    }

    public void GoBackToExplore()
    {
        TransitionTo(AppState.EXPLORE);
    }

    public void GoBackToScan()
    {
        TransitionTo(AppState.SCAN);
    }
    
    public void GoHome()
    {
        TransitionTo(AppState.HOME);
    }

    private void TransitionTo(AppState newState)
    {
        currentState = newState;
        UpdateScreens();
    }

    private void UpdateScreens()
    {
        if (homeScreen) homeScreen.SetActive(currentState == AppState.HOME);
        if (scanScreen) scanScreen.SetActive(currentState == AppState.SCAN);
        if (analyzingScreen) analyzingScreen.SetActive(currentState == AppState.ANALYZING);
        if (identifiedScreen) identifiedScreen.SetActive(currentState == AppState.IDENTIFIED);
        if (uncertainScreen) uncertainScreen.SetActive(currentState == AppState.UNCERTAIN);
        if (unknownScreen) unknownScreen.SetActive(currentState == AppState.UNKNOWN);
        if (errorScreen) errorScreen.SetActive(currentState == AppState.ERROR);
        if (exploreScreen) exploreScreen.SetActive(currentState == AppState.EXPLORE);
        
        if (currentState != AppState.PLANT_DETAILS && plantDetailsController != null)
        {
            plantDetailsController.gameObject.SetActive(false);
        }
    }
}
