using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

[ExecuteAlways] 
public class BoardGenerator : MonoBehaviour
{
    private VisualElement root;
    private VisualElement boardContainer;
    private List<VisualElement> slots = new List<VisualElement>();
    private bool isGenerating = false;
    private bool isGameOver = false;
    private int moveCount = 0;
    private float elapsedTime = 0f;
    private bool timerRunning = false;
    private bool gameStarted = false;
    private Label movesLabel;
    private Label timeLabel;
    private VisualElement winModal;
    private Label modalMovesLabel;
    private VisualElement playAgainBtn;
    private VisualElement pauseModal;
    private VisualElement pausePlayBtn;
    private VisualElement pauseHistoryBtn;
    private VisualElement pauseIconBtn;
    private bool pauseGame = false;
    private bool pauseMenu = false;
    private float lastToggleTime;
    private const float toggleCooldown = 0.3f;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            return; 
        }

        root.focusable = true;
        root.Focus();
        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);

        var bgContainer = root.Q<VisualElement>("bg-title");
        bgContainer.SendToBack();
        
        var bgWaves = root.Q<VisualElement>("bg-wavelines");
        bgWaves.SendToBack();

        var bgLabel = root.Q<Label>("bg-label");
        bgLabel.pickingMode = PickingMode.Ignore;

        VisualElement newGameBtn = root.Q<VisualElement>("NewGameButton");

        if (newGameBtn == null)
        {
            return;
        }

        newGameBtn.RegisterCallback<ClickEvent>(evt => ResetGame());

        boardContainer = root.Q<VisualElement>("Grid");

        if (boardContainer == null)
        {
            return;
        }

        VisualElement valueParent = root.Q<VisualElement>("Value");

        if (valueParent != null)
        {
            movesLabel = valueParent.Q<Label>("Moves"); 
            timeLabel = valueParent.Q<Label>("Times"); 
        }

        winModal = root.Q<VisualElement>("WinModal");

        if (winModal != null)
        {
            modalMovesLabel = winModal.Q<Label>("moveslabel");
            
            playAgainBtn = winModal.Q<VisualElement>("button").Q<VisualElement>();

            if (playAgainBtn != null)
            {
                playAgainBtn.RegisterCallback<ClickEvent>(evt => closeWinModal());
            }

            winModal.style.display = DisplayStyle.None;
        }

        pauseModal = root.Q<VisualElement>("PauseModal");
        if( pauseModal!= null){
            pauseHistoryBtn = pauseModal.Q<VisualElement>("history-btn");
            pausePlayBtn = pauseModal.Q<VisualElement>("play-btn");
            pauseIconBtn = pauseModal.Q<VisualElement>("icon-btn");

            pausePlayBtn.RegisterCallback<ClickEvent>(evt => continuePlay());
            pauseModal.style.display = DisplayStyle.None;
        }
        ResetGame();
    }
    
    void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimeDisplay();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // if hasFocus is false, it means the user clicked away or alt-tabbed
        if (!hasFocus && !isGameOver && !isGenerating && !pauseMenu)
        {
            showPauseMenu();
        }

        if (hasFocus)
        {
            root?.MarkDirtyRepaint();
        }
    }

    private void OnNavigationMove(NavigationMoveEvent evt)
    {
        if (isGenerating || isGameOver || pauseGame)
        {
            evt.StopPropagation();
            return;
        }

        int emptyIndex = -1;

        // 1. Find the current empty slot
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].childCount == 0) { emptyIndex = i; break; }
        }

        if (emptyIndex == -1) return;

        // 2. Calculate the index of the tile that should move
        int x = emptyIndex % 4;
        int y = emptyIndex / 4;
        int targetIndex = -1;

        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Up:    if (y < 3) targetIndex = emptyIndex + 4; break;
            case NavigationMoveEvent.Direction.Down:  if (y > 0) targetIndex = emptyIndex - 4; break;
            case NavigationMoveEvent.Direction.Left:  if (x < 3) targetIndex = emptyIndex + 1; break;
            case NavigationMoveEvent.Direction.Right: if (x > 0) targetIndex = emptyIndex - 1; break;
        }

        // 3. If a valid neighbor is found, "click" it
        if (targetIndex != -1 && targetIndex < slots.Count)
        {
            Button tileToMove = slots[targetIndex].Q<Button>();
            if (tileToMove != null)
            {
                AttemptMove(tileToMove);
            }
        }
        
        // Prevent the UI from trying to move focus to other random buttons
        evt.StopPropagation();
    }

    void showPauseMenu(){
        if (Time.unscaledTime - lastToggleTime < toggleCooldown) return;
        lastToggleTime = Time.unscaledTime;
        if(pauseMenu){
            pauseModal.style.display = DisplayStyle.None;
            pauseMenu = false;
            pauseTheGame();
            root.Focus();
        }else{
            pauseMenu = true;
            pauseTheGame();
            VisualElement content = pauseModal.Q<VisualElement>(className: "pause-modal-content");
            if (content != null) PlayBounceInUp(content, 200);
            pauseModal.style.display = DisplayStyle.Flex;
        }
    }

    void continuePlay(){
        if(!pauseMenu) return;
        pauseModal.style.display = DisplayStyle.None;
        pauseMenu = false;
        pauseTheGame();
        root.Focus();
    }

    void pauseTheGame(){
        if (!gameStarted && !pauseGame) return;
        
        if(pauseGame){
            timerRunning = true;
            gameStarted = true;
            pauseGame = false;
            return;
        }
        timerRunning = false;
        gameStarted = false;
        pauseGame = true;
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (isGenerating || isGameOver)
        {
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.Escape)
        {
            showPauseMenu();
            evt.StopPropagation();
        }

        if (evt.keyCode == KeyCode.Space)
        {
            showPauseMenu();
            evt.StopPropagation();
        }
    }

    void UpdateTimeDisplay()
    {
        if (timeLabel == null) return;
        
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timeLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void ResetStats()
    {
        moveCount = 0;
        elapsedTime = 0f;
        timerRunning = false;
        gameStarted = false;

        if (movesLabel != null) movesLabel.text = "0";
        if (timeLabel != null) timeLabel.text = "00:00";
    }

    void ResetGame()
    {
        if (isGenerating) return;
        
        ResetStats();
        
        StartCoroutine(GenerateBoardWithLock());
    }

    void CheckWinCondition()
    {
        bool allCorrect = true;
        for (int i = 0; i < 15; i++)
        {
            Button tile = slots[i].Q<Button>();
            if (tile == null || !tile.ClassListContains("correct-position"))
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect && gameStarted)
        {
            isGameOver = true;
            timerRunning = false;
            gameStarted = false;
            ShowWinModal();
        }
    }

    void ShowWinModal()
    {
        if (winModal == null) return;

        if (modalMovesLabel != null) 
            modalMovesLabel.text = $"{moveCount} moves";

        winModal.style.display = DisplayStyle.Flex;

        VisualElement content = winModal.Q<VisualElement>(className: "modal-content");
        if (content != null) PlayBounceInUp(content, 200);
    }

    void closeWinModal(){
        isGameOver = false;
        ResetGame();
        if (winModal != null) 
            {
                winModal.style.display = DisplayStyle.None;
        }
    }

    System.Collections.IEnumerator GenerateBoardWithLock()
    {
        isGenerating = true;

        boardContainer.style.visibility = Visibility.Hidden;
        boardContainer.Clear();
        slots.Clear();

        List<int> numbers = GetDerangedShuffle(15);

        for (int i = 0; i < 16; i++)
        {
            VisualElement slot = new VisualElement();
            slot.AddToClassList("slot");
            
            int currNumber = numbers[i];
            
            if (currNumber != 0) 
            {
                Button tile = new Button();
                tile.userData = i;
                tile.AddToClassList("tile");
                tile.clicked += () => AttemptMove(tile);

                VisualElement ball1 = new VisualElement { pickingMode = PickingMode.Ignore };
                ball1.AddToClassList("ball-1");
                VisualElement ball2 = new VisualElement { pickingMode = PickingMode.Ignore };
                ball2.AddToClassList("ball-2");
                Label shadowLabel = new Label(currNumber.ToString()) { pickingMode = PickingMode.Ignore };
                shadowLabel.AddToClassList("tile-shadow"); 
                Label numberLabel = new Label(currNumber.ToString()) { pickingMode = PickingMode.Ignore };
                numberLabel.AddToClassList("tile-text");

                tile.Add(ball1);
                tile.Add(ball2);
                tile.Add(shadowLabel);
                tile.Add(numberLabel);

                UpdateTileColor(tile, currNumber, i);
                slot.Add(tile);
            }

            boardContainer.Add(slot);
            slots.Add(slot);
        }

        boardContainer.style.visibility = Visibility.Visible;

        foreach (var slot in slots)
        {
            Button t = slot.Q<Button>();
            if (t != null) PlayBounceIn(t); 
        }

        yield return new WaitForSeconds(0.75f);

        isGenerating = false;
    }

    void PlayBounceInUp(VisualElement e, int delayMs)
    {
        e.style.opacity = 0;
        e.style.translate = new Translate(0, 500, 0);
        e.style.scale = new Scale(new Vector3(0, 0, 1));

        e.schedule.Execute(() => {

            e.experimental.animation.Start(new Vector3(0, 500, 0), new Vector3(0, -20, 0), 400, (el, v) => {
                el.style.translate = new Translate(v.x, v.y, 0);
                el.style.opacity = 1.0f;
                el.style.scale = new Scale(new Vector3(1, 1, 1)); // Pop to full scale
            }).Ease(Easing.OutCubic).OnCompleted(() => {

                e.experimental.animation.Start(new Vector3(0, -20, 0), new Vector3(0, 10, 0), 150, 
                    (el, v) => el.style.translate = new Translate(v.x, v.y, 0)).Ease(Easing.InOutCubic).OnCompleted(() => {

                    e.experimental.animation.Start(new Vector3(0, 10, 0), new Vector3(0, -5, 0), 150, 
                        (el, v) => el.style.translate = new Translate(v.x, v.y, 0)).Ease(Easing.InOutCubic).OnCompleted(() => {

                        e.experimental.animation.Start(new Vector3(0, -5, 0), Vector3.zero, 300, 
                            (el, v) => el.style.translate = new Translate(v.x, v.y, 0)).Ease(Easing.OutBack);
                    });
                });
            });
        }).StartingIn(delayMs);
    }

    void PlayBounceIn(VisualElement e)
    {
        e.experimental.animation.Start(new Vector3(0, 0, 1), new Vector3(1.1f, 1.1f, 1), 150, (el, v) => {
            el.style.scale = new Scale(v);
            el.style.opacity = 1.0f;
        }).Ease(Easing.OutCubic).OnCompleted(() => {

            e.experimental.animation.Start(new Vector3(1.1f, 1.1f, 1), new Vector3(0.9f, 0.9f, 1), 150, 
                (el, v) => el.style.scale = new Scale(v)).Ease(Easing.InOutCubic).OnCompleted(() => {

                e.experimental.animation.Start(new Vector3(0.9f, 0.9f, 1), new Vector3(1.03f, 1.03f, 1), 150, 
                    (el, v) => el.style.scale = new Scale(v)).Ease(Easing.InOutCubic).OnCompleted(() => {

                    e.experimental.animation.Start(new Vector3(1.03f, 1.03f, 1), new Vector3(1f, 1f, 1), 300, 
                        (el, v) => el.style.scale = new Scale(v)).Ease(Easing.OutBack);
                });
            });
        });
    }

    void AttemptMove(Button tile)
    {
        if (isGenerating) return;

        int fromIndex = (int)tile.userData; 
        int emptyIndex = -1;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].childCount == 0) { emptyIndex = i; break; }
        }
        
        if (emptyIndex != -1 && IsAdjacent(fromIndex, emptyIndex))
        {
            if (!gameStarted)
            {
                gameStarted = true;
                timerRunning = true; 
            }

            moveCount++;
            if (movesLabel != null) movesLabel.text = moveCount.ToString();

            slots[emptyIndex].Add(tile);
            tile.userData = emptyIndex;
            
            PlayBounceIn(tile);
            
            int newIndex = emptyIndex;

            Label label = tile.Q<Label>(className: "tile-text");
            if (label != null)
            {
                UpdateTileColor(tile, int.Parse(label.text), emptyIndex);
                CheckWinCondition();
            }
                
        }
    }

    void UpdateTileColor(Button tile, int number, int index)
    {
        tile.RemoveFromClassList("correct-position");
        tile.RemoveFromClassList("wrong-position");

        if (number == index + 1)
            tile.AddToClassList("correct-position");
        else
            tile.AddToClassList("wrong-position");
    }

    bool IsAdjacent(int i1, int i2)
    {
        int x1 = i1 % 4, y1 = i1 / 4;
        int x2 = i2 % 4, y2 = i2 / 4;
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }

    List<int> GetDerangedShuffle(int size)
    {
        List<int> numbers = new List<int>();
        for (int n = 1; n <= 15; n++) numbers.Add(n);
        numbers.Add(0);

        int safetyBreak = 0;
        do
        {
            ShuffleList(numbers);
            safetyBreak++;
        } while ((HasMatch(numbers) || !IsSolvable(numbers)) && safetyBreak < 1000);

        return numbers;
    }

    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    bool HasMatch(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != 0 && list[i] == i + 1) return true;
        }
        return false;
    }

    bool IsSolvable(List<int> list)
    {
        int inversions = 0;
        int emptyRowFromBottom = 0;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == 0)
            {
                emptyRowFromBottom = 4 - (i / 4);
                continue;
            }

            for (int j = i + 1; j < list.Count; j++)
            {
                if (list[j] != 0 && list[i] > list[j])
                {
                    inversions++;
                }
            }
        }

        if (emptyRowFromBottom % 2 == 0)
            return (inversions % 2 != 0);
        else
            return (inversions % 2 == 0);
    }
}