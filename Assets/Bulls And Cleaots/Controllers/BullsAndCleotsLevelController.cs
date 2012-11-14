using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BCLevelData {
    public int solutionLength;
    public Color[] possibleColors;
    public int[] possibleNumbers;
    public int[] solutionNumbers;
    public Color[] solutionColors;
    
    public BCLevelData(int len, IEnumerable<int> numbers, IEnumerable<Color> colors){
        solutionLength = len;
        possibleColors = colors.ToArray();
        possibleNumbers = numbers.ToArray();
        
        solutionColors = possibleColors.AsEnumerable().OrderBy(x=>Random.value)
                .Take(solutionLength).ToArray();
       
        solutionNumbers = possibleNumbers.AsEnumerable()
            .OrderBy(x=>Random.value)
                .Take(solutionLength).ToArray();
    }
}


public class BullsAndCleotsLevelController : MonoBehaviour, IEventListener {

    public Rect attemptButtonRect;

    public SolutionManager solution;

    public GameObject numberedBlocks;
    public GameObject coloredBlocks;
    public GameObject inputPane;
    
    static string onSnapName = typeof(DraggableOnSnapEvent).ToString();
    static string snapOnEnterName = typeof(SnappableOnEnterEvent).ToString();
    static string snapOnExitName = typeof(SnappableOnExitEvent).ToString();
    static string onMoveName = typeof(DraggableOnMoveEvent).ToString();
 
   
    private string[] handledEventNames = {
        onSnapName,
        onMoveName,
        snapOnEnterName,
        snapOnExitName,

    };
 
    private bool hasWon;
    private Rect messagesRect = new Rect(10, 95, 300, 250);
    private int maxMessages = 10;
    private Queue<string> playerMessageQueue;
 

    private int attemptCount;


    private BCLevelData initData = null;
    
    public BCLevelData InitData{
        set {
            if (initData != null){
                Debug.LogWarning("Level data already set!");
                return;
            }
            initData = value;
        }
    }
        

  
    void Start() {

        // init vars
        hasWon = false;
        attemptCount = 0;
        playerMessageQueue = new Queue<string>(maxMessages);

        solution = new SolutionManager(initData.solutionLength);
        solution.Colors = initData.solutionColors;
        solution.Digits = initData.solutionNumbers;


        
        numberedBlocks = Instantiate(numberedBlocks) as GameObject;
        numberedBlocks.GetComponent<NumberedBlocks>().PossibleNumbers = initData.possibleNumbers;
		numberedBlocks.transform.parent = transform;
        
        coloredBlocks = Instantiate(coloredBlocks) as GameObject;
        coloredBlocks.GetComponent<ColoredBlocks>().PossibleColors = initData.possibleColors;
		coloredBlocks.transform.parent = transform;
        
        inputPane = Instantiate(inputPane) as GameObject;
        inputPane.GetComponent<SolutionInputPanel>().solutionLength = initData.solutionLength;
		inputPane.transform.parent = transform;
        
        // register as listener for desired events
        foreach (string eventName in handledEventNames) {
            EventManager.instance.RegisterListener(this, (string)eventName);
        }

    }

    public bool HandleEvent(IEvent evnt) {
        string eventName = evnt.GetName();
        object eventData = evnt.GetData();

        if (eventName == onSnapName) {

            GuessDropped(eventData as GameObject);
        }
        else if (eventName == onMoveName) {
            // ignore
        }
        else if (eventName == snapOnEnterName) {
            // ignore
        }
        else if (eventName == snapOnExitName) {
  
            GuessVacate(eventData as GameObject);

        }
        else {
            Debug.LogWarning("Unexpected Event: " + eventName);
            return false;
        }
        return true;
    }

 

    void PlayerAttemptSolve() {

        if (solution.HasBlankGuesses) {
            // We don't allow for there to be any unfilled 
            // guess slots because that would make the game 
            // too easy. So complain to the user.
            AddPlayerMessage("Blank Guesses!");
            return;

        }

        int bullsCount = solution.BullsCount();
        int cleotsCount = solution.CleotsCount();


        attemptCount++;


        AddPlayerMessage(BuildAttemptMessage(attemptCount, bullsCount, cleotsCount));





        // TODO.D: Make this sane. Couple the <x>Bar with the level controller.
        // These do not need to be and should not be events. I got 
        // caught using my shiny new hammer to do something better
        // suited to a screwdriver you might say.
        EventManager.instance.RelayEvent(new BullsFoundEvent(bullsCount));
        EventManager.instance.RelayEvent(new CleotsFoundEvent(cleotsCount));
        EventManager.instance.RelayEvent(new SolutionMixEvent(solution.DigitCount()));

        if (bullsCount == solution.SolutionLength) {
            // winDigits == guessDigits, so the player has guessed
            // the correct number.
            hasWon = true;
            AddPlayerMessage("You win!");
            
        }
        else{

            numberedBlocks.GetComponent<NumberedBlocks>().Reset();
            coloredBlocks.GetComponent<ColoredBlocks>().Reset();
        }
    }

    string BuildAttemptMessage(int attemptCount, int bullsCount, int cleotsCount) {

        string attemptStatusMessage = "";
        string messageFormat = "#{0} ({1}): {2} Bulls, {3} Cleots";


        // Example:
        //#1 (1234): 2 Bulls, 1 Cleots 
        attemptStatusMessage = string.Format(messageFormat,
                attemptCount,
                solution,
                bullsCount, cleotsCount);

        return attemptStatusMessage;
    }

    // Adds the message to the message queue. If there are 
    // more than maxMessageCount messages in the queue 
    // then remove excess messages.
    void AddPlayerMessage(string message) {
        playerMessageQueue.Enqueue(message);
        while (playerMessageQueue.Count > maxMessages) {
            playerMessageQueue.Dequeue();
        }
    }


    void PlayerEndGame() {
        if (solution.DigitCount() == 0)
            MiniGameController.endMiniGame(
                BullsAndCleots.ExitEdges.IncreaseColors);
        
        else if (solution.ColorCount() == 0)
            MiniGameController.endMiniGame(
                BullsAndCleots.ExitEdges.IncreaseNumbers);
        
        else 
            MiniGameController.endMiniGame(
                BullsAndCleots.ExitEdges.IncreaseDifficulty);
    }

    
    
    // Account for a color block being dropped to the guess pane.
    void GuessDropped(GameObject obj) {
        Draggable draggable = obj.GetComponent<Draggable>();
        Snappable snappable = draggable.currentSnappable.GetComponent<Snappable>();

        SolutionBlock solutionBlock = obj.GetComponent<SolutionBlock>();
        SolutionSnapArea dropArea = draggable.currentSnappable.GetComponent<SolutionSnapArea>();
        
      int index = dropArea.Index;
      object guess = solutionBlock.data;

        if (snappable.isAlreadyOccupied) {
            object temp = solution.Guesses[index];
            draggable.Reset();
            solution.SetGuessAt(index, temp);


        }
        else {
            solution.SetGuessAt(index, guess);
           

        }

    }

   
    
    void GuessVacate(GameObject obj) {
        Draggable d = obj.GetComponent<Draggable>();
        SolutionBlock solutionBlock = obj.GetComponent<SolutionBlock>();

        SolutionSnapArea dropArea = d.currentSnappable.GetComponent<SolutionSnapArea>();
        int index = dropArea.Index;

        object vacatingGuess = solutionBlock.data;

        object currentGuess = solution.Guesses[index];
            
        // Guess vacating because snaparea already occupied so
        // keep current guess.
        if (currentGuess != vacatingGuess)
            return;
        

        
        // Otherwise it means that the snap area will be empty
        // after the guess vacates so set it to NOT_GUESSED
        solution.ResetGuessAt(index);



    }

    void drawAttemptButton() {
        if (GUI.Button(attemptButtonRect, "Attempt")) {
            PlayerAttemptSolve();
        }
    }

    void drawWinButton() {
        if (GUI.Button(attemptButtonRect, "You Win!")) {
            PlayerEndGame();
        }
    }


    // Displays the latest maxMessageCount> messages in a text box 
    // to the player.
    void drawMessages() {
        GUI.TextArea(messagesRect,
            string.Join("\n", playerMessageQueue.ToArray()));
    }

    void OnGUI() {

        if (!hasWon) {
            drawAttemptButton();
        }
        else {
            drawWinButton();

        }

        drawMessages();
    }
}
