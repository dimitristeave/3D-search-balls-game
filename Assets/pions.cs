using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;


public class Pions : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int depth = 3;
    public Sprite playerPieceSprite; // Sprite représentant la pièce du joueur
    public Sprite uiSprite;
    public Sprite aiPieceSprite; // Sprite représentant la pièce de l'IA
    public Button[] boardButtons; // Tableau de tous les boutons/cases du plateau
    public int maxPlayerPieces = 12; // Nombre maximum de pièces que le joueur peut placer
    public TextMeshProUGUI playerRestText;
    public TextMeshProUGUI aiRestText;
    public TextMeshProUGUI commentText;

    private int playerPiecesPlaced = 0; // Nombre de pièces placées par le joueur
    private int playerAIPiecesPlaced = 0;
    private Button draggedButton; // Référence au bouton qui est en train d'être déplacé
    private Image draggedImage; // Référence à l'image du bouton déplacé
    private bool isPlayersTurn = true; // Variable pour suivre de quel joueur c'est le tour

    // Liste pour stocker les indices des boutons contenant les pions sur le plateau
    private List<int> aiPieceIndices = new List<int>();
    private List<int> playerPieceIndices = new List<int>();
    // Liste pour stocker les indices des boutons contenant les pions capturés
    private List<int> aiPieceCapture = new List<int>();
    private List<int> playerPieceCapture = new List<int>();
    Capture capture ;
    private static int playerRestPions = 12;
    private static int aiRestPions = 12;
    List<int> borderNumbers = new List<int>() { 0, 1, 2, 3, 4, 5, 9, 10, 14, 15, 19, 20, 24, 25, 26, 27, 28, 29 };

    void Start()
    {
        // Assignez un index unique à chaque bouton dans le tableau boardButtons
        for (int i = 0; i < boardButtons.Length; i++)
        {
            Button button = boardButtons[i];
            button.onClick.AddListener(() => PlacePiece(button)); // Utilisez une expression lambda pour passer le bouton à la fonction PlacePiece

            // Ajoutez la propriété buttonIndex à chaque bouton pour stocker son index dans le tableau
            button.gameObject.AddComponent<ClickableButton>().buttonIndex = i;

            // Ajoutez un gestionnaire d'événements de début de glissement à chaque bouton du plateau
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => OnBeginDrag((PointerEventData)data));
            trigger.triggers.Add(beginDragEntry);

            // Ajoutez un gestionnaire d'événements de glissement à chaque bouton du plateau
            EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            // Ajoutez un gestionnaire d'événements de fin de glissement à chaque bouton du plateau
            EventTrigger.Entry endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener((data) => OnEndDrag((PointerEventData)data));
            trigger.triggers.Add(endDragEntry);
        }

        // Commencez le jeu en tant que joueur
        isPlayersTurn = true;
    }

    // Fonction appelée au début du glissement
    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedButton = eventData.pointerPress.GetComponent<Button>(); // Obtenez le bouton en train d'être déplacé
        draggedImage = draggedButton.GetComponent<Image>(); // Obtenez l'image du bouton déplacé
    }

    // Fonction appelée pendant le glissement
    public void OnDrag(PointerEventData eventData)
    {
        // Mettez à jour la position de l'image du bouton déplacé pour suivre le mouvement du glissement
        draggedImage.rectTransform.position = eventData.position;
    }

    // Fonction appelée à la fin du glissement
    public void OnEndDrag(PointerEventData eventData)
    {
        // Si aucun bouton n'est en cours de glissement, ne rien faire
        if (draggedButton == null) return;

        // Réinitialisez la position de l'image du bouton déplacé
        draggedImage.rectTransform.localPosition = Vector2.zero;

        // Obtenez l'image du bouton déplacé
        Image draggedButtonImage = draggedButton.GetComponent<Image>();

        // Si le bouton d'origine contient un pion, procédez au déplacement
        if (draggedButtonImage.sprite != null)
        {
            // Recherchez le bouton survolé par la position actuelle du curseur
            foreach (Button button in boardButtons)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(buttonRect, Input.mousePosition))
                {
                    // Si une case voisine est survolée et qu'elle est vide, déplacez le pion
                    ClickableButton clickableButton = button.GetComponent<ClickableButton>();
                    Image buttonImage = button.GetComponent<Image>();
                    PlayerCapture playerCapture = IsPlayerCapture(draggedButton, clickableButton);
                    capture = AiHasPotentialCapture();
                    // Si la case est vide, déplacez le pion
                    if (IsAdjacent(draggedButton, clickableButton) && !playerPieceIndices.Contains(clickableButton.buttonIndex) &&
                        !aiPieceIndices.Contains(clickableButton.buttonIndex))
                    {
                        commentText.text = "Player is moving from "+draggedButton.name+ " to "+button.name;
                        MovePiece(draggedButton, clickableButton);
                        playerPieceIndices.Remove(draggedButton.GetComponent<ClickableButton>().buttonIndex);
                        playerPieceIndices.Add(clickableButton.buttonIndex);
                        isPlayersTurn = !isPlayersTurn;
                        if (aiPieceIndices.Count == 0)
                        {
                            PlayAI();
                        }
                        if (capture.score == +1000 || capture.score == -1000)
                        {
                            PlayMoveAI();
                        }
                        else if (capture.score != +1000 && capture.score != -1000)
                        {
                            PlayAI();
                        }
                        if (playerAIPiecesPlaced == maxPlayerPieces)
                        {
                            PlayMoveAI();
                        }
                        PlayMoveAI();

                    }
                    else if (IsOneCaseAdjacent(draggedButton, clickableButton) && !playerPieceIndices.Contains(clickableButton.buttonIndex) &&
                        !aiPieceIndices.Contains(clickableButton.buttonIndex) && IsPlayerCapture(draggedButton, clickableButton).isCapture)
                    {                      
                        MovePiece(draggedButton, clickableButton);
                        playerPieceIndices.Remove(draggedButton.GetComponent<ClickableButton>().buttonIndex);
                        playerPieceIndices.Add(clickableButton.buttonIndex);
                        Image image = boardButtons[IsPlayerCapture(draggedButton, clickableButton).buttonCapturedIndex].GetComponent<Image>();
                        image.sprite = null;
                        SetButtonInteractable(boardButtons[IsPlayerCapture(draggedButton, clickableButton).buttonCapturedIndex], true);
                        aiPieceIndices.Remove(IsPlayerCapture(draggedButton, clickableButton).buttonCapturedIndex);
                        foreach (int aiPieceIndex in aiPieceIndices)
                        {

                            Button buttonChoosen = boardButtons[aiPieceIndex];
                            SetButtonInteractable(buttonChoosen, true);
                            buttonChoosen.onClick.AddListener(() => ChoosePiece(buttonChoosen));
                            SetButtonInteractable(buttonChoosen, true);
                            commentText.text = "Player is catching " + buttonChoosen.name + " from" + draggedButton.name + " to " + button.name;
                        }
                        
                        aiRestPions -= 2;
                        if (aiRestPions < 0)
                        {
                            aiRestPions = 0;
                        }
                        aiRestText.text = aiRestPions.ToString();
                        isPlayersTurn = !isPlayersTurn;
                        if (aiPieceIndices.Count == 0)
                        {
                            PlayAI();
                        }
                        if (capture.score == +1000 || capture.score == -1000)
                        {
                            PlayMoveAI();
                        }
                        else if (capture.score != +1000 && capture.score != -1000)
                        {
                            PlayAI();
                        }
                        if (playerAIPiecesPlaced == maxPlayerPieces)
                        {
                            PlayMoveAI();
                        }
                        PlayMoveAI();

                    }
                    else
                    {
                        commentText.text = "this way is not neighbour or free ";
                    }
                    break;
                }
            }
        }
        else
        {
            commentText.text = "The original button does not contain a counter";
        }

        draggedButton = null;
        draggedImage = null;
    }

    // Fonction pour vérifier si deux boutons sont voisins (cases orthogonales seulement)
    private bool IsAdjacent(Button button1, ClickableButton button2)
    {
        int buttonIndex1 = button1.GetComponent<ClickableButton>().buttonIndex;
        int buttonIndex2 = button2.buttonIndex;

        // Vérifiez si les indices des boutons sont adjacents
        return AreNeighbours(buttonIndex1, buttonIndex2);
    }

    private bool IsOneCaseAdjacent(Button button1, ClickableButton button2)
    {
        int buttonIndex1 = button1.GetComponent<ClickableButton>().buttonIndex;
        int buttonIndex2 = button2.buttonIndex;

        // Vérifiez si les indices des boutons sont adjacents
        return AreOneCaseNeighbours(buttonIndex1, buttonIndex2);
    }

    public struct PlayerCapture
    {
        public bool isCapture;
        public int buttonCapturedIndex;
    }

    private PlayerCapture IsPlayerCapture(Button button1, ClickableButton button2)
    {
        PlayerCapture playerCapture = new PlayerCapture();
        int buttonIndex1 = button1.GetComponentInParent<ClickableButton>().buttonIndex;
        int buttonIndex2 = button2.buttonIndex;
        if (IsOneCaseAdjacent(button1, button2))
        {
            List<int> neighboursButton1 = GetNeighbourIndicesAi(buttonIndex1);
            List<int> neighboursButton2 = GetNeighbourIndicesAi(buttonIndex2);
            foreach (int neighbourIndex1 in neighboursButton1)
            {
                foreach (int neighbourIndex2 in neighboursButton2)
                {
                    if (neighbourIndex1 == neighbourIndex2)
                    {
                        playerCapture.isCapture = true;
                        playerCapture.buttonCapturedIndex = neighbourIndex1;
                        return playerCapture;
                    }
                }
            }
        }
        playerCapture.isCapture = false;
        return playerCapture;
    }

    // Fonction pour déplacer un pion d'une case à une autre
    private void MovePiece(Button fromButton, ClickableButton toButton)
    {
        // Obtenez l'image des deux boutons
        Image fromImage = fromButton.GetComponent<Image>();
        Image toImage = toButton.GetComponent<Image>();

        // Déplacez le sprite du pion vers la nouvelle case
        toImage.sprite = fromImage.sprite;

        // Effacez le sprite de l'ancienne case
        fromImage.sprite = null;
        SetButtonInteractable(fromButton, true);

        // Rendez le bouton toButton non interactif
        Button button = toButton.GetComponent<Button>();
        SetButtonInteractable(button, false);
    }

    void ChoosePiece(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.sprite = null;
        aiPieceIndices.Remove(button.GetComponent<ClickableButton>().buttonIndex);
    }

    // Fonction pour placer une pièce lorsqu'un bouton est cliqué
    void PlacePiece(Button button)
    {
        Capture capture = AiHasPotentialCapture();
        Image buttonImage = button.GetComponent<Image>(); // Obtenez le composant Image du bouton
        if (isPlayersTurn && (buttonImage.sprite == uiSprite || buttonImage.sprite == null))
        {
            // Vérifiez si le joueur a déjà placé le nombre maximum de pièces
            if (isPlayersTurn && playerPiecesPlaced >= maxPlayerPieces)
            {
                commentText.text = "You placed a maximimum counters on the panel!";
                return;
            }

            // Vérifiez de quel joueur est le tour et attribuez le sprite approprié à l'image du bouton
            buttonImage.sprite = playerPieceSprite;
            playerPiecesPlaced++; // Incrémente le compteur de pièces placées par le joueur
            playerPieceIndices.Add(button.GetComponent<ClickableButton>().buttonIndex);
            commentText.text = "Player put "+button.name;

            // Changez de tour
            isPlayersTurn = !isPlayersTurn;
            SetButtonInteractable(button, false);

            if (aiPieceIndices.Count == 0)
            {
                PlayAI();
            }
            if (capture.score == +1000 || capture.score == -1000)
            {
                PlayMoveAI();
            }
            else if (capture.score != +1000 && capture.score != -1000)
            {
                PlayAI();
            }
            if (playerAIPiecesPlaced == maxPlayerPieces)
            {
                PlayMoveAI();
            }
        }
        

    }


    // Fonction pour activer ou désactiver l'interaction avec un bouton
    private void SetButtonInteractable(Button button, bool interactable)
    {
        button.interactable = interactable;
    }

    // Fonction pour vérifier si deux cases sont voisines
    private bool AreNeighbours(int buttonIndex1, int buttonIndex2)
    {
        // Vérifiez si les indices des cases sont valides (dans la plage de 0 à 29)
        if (buttonIndex1 < 0 || buttonIndex1 >= 30 || buttonIndex2 < 0 || buttonIndex2 >= 30)
        {
            return false;
        }

        // Vérifiez si les cases sont côte à côte dans la même rangée
        if (Mathf.Abs(buttonIndex1 - buttonIndex2) == 1 && buttonIndex1 / 5 == buttonIndex2 / 5)
        {
            return true;
        }
        // Vérifiez si les cases sont côte à côte dans la même colonne
        else if (Mathf.Abs(buttonIndex1 - buttonIndex2) == 5)
        {
            return true;
        }

        return false;
    }

    private bool AreOneCaseNeighbours(int buttonIndex1, int buttonIndex2)
    {
        // Vérifiez si les indices des cases sont valides (dans la plage de 0 à 29)
        if (buttonIndex1 < 0 || buttonIndex1 >= 30 || buttonIndex2 < 0 || buttonIndex2 >= 30)
        {
            return false;
        }

        // Vérifiez si les cases sont côte à côte dans la même rangée
        if (Mathf.Abs(buttonIndex1 - buttonIndex2) == 2 && buttonIndex1 / 5 == buttonIndex2 / 5)
        {
            return true;
        }
        // Vérifiez si les cases sont côte à côte dans la même colonne
        else if (Mathf.Abs(buttonIndex1 - buttonIndex2) == 10)
        {
            return true;
        }

        return false;
    }

    // Fonction pour que l'IA joue de manière aléatoire
    private void PlayAI()
    {
        Place place = PlacePions();
        if (!isPlayersTurn && playerAIPiecesPlaced < maxPlayerPieces)
        {          
            int indexPlace = place.placeIndex;
            // Place le pion de l'IA sur le bouton choisi aléatoirement
            Button button = boardButtons[indexPlace];
            Image buttonImage = button.GetComponent<Image>();
            buttonImage.sprite = aiPieceSprite;
            playerAIPiecesPlaced++;
            aiPieceIndices.Add(indexPlace);
            commentText.text = "AI put " + button.name;
            // Désactive l'interaction avec le bouton pour empêcher le joueur de le déplacer
            button.interactable = false;

            // Change de tour pour le joueur
            isPlayersTurn = true;
        }

    }

    private void PlayMoveAI()
    {      
        Capture capture = AiHasPotentialCapture();
        if (!isPlayersTurn)
        {
            // Vérifiez s'il y a des pions de l'IA sur le plateau
            if (aiPieceIndices.Count > 0)
            {
                int aiPieceIndex = capture.aiPlayerIndex;
                int neighbourIndex = capture.neighbourIndex;
                int neighbourCapturedIndex = capture.neighbourCapturedIndex;
                if (capture.score == -1000)
                {
                    Button fromButton = boardButtons[aiPieceIndex];
                    Button toButton = boardButtons[neighbourIndex];
                    commentText.text = "AI go away from " + fromButton + " to " + toButton;
                    MovePiece(fromButton, toButton.GetComponent<ClickableButton>());
                    aiPieceIndices.Remove(aiPieceIndex);
                    aiPieceIndices.Add(neighbourIndex);
                }
                else if (capture.score == +1000)
                {
                    // Déplacez le pion de l'IA vers la case voisine choisie
                    Button fromButton = boardButtons[aiPieceIndex];
                    Button toButton = boardButtons[neighbourIndex];
                    Button buttonCaptured = boardButtons[neighbourCapturedIndex];
                    MovePiece(fromButton, toButton.GetComponent<ClickableButton>());
                    int pieceChoosen = AiChooseOnePiece();
                    aiPieceIndices.Remove(aiPieceIndex);
                    playerPieceIndices.Remove(neighbourCapturedIndex);
                    aiPieceIndices.Add(neighbourIndex);
                    playerPieceCapture.Add(neighbourCapturedIndex);
                    playerPieceCapture.Add(pieceChoosen);
                    Image image = buttonCaptured.GetComponent<Image>();
                    image.sprite = null;
                    SetButtonInteractable(buttonCaptured, true);
                    commentText.text = "IA is moving from " + aiPieceIndex + " to " + neighbourIndex + ", cought " + neighbourCapturedIndex + " and choosed " + pieceChoosen;
                    playerRestPions -= 2;
                    if (playerRestPions < 0)
                    {
                        playerRestPions = 0;
                    }
                    playerRestText.text = playerRestPions.ToString();
                    /*if (playerPieceCapture.Count == 12)
                    {
                        commentText.text = "AI winner !";
                        Debug.Log("AI winner !");
                    }*/
                }
                else if (capture.score == +500)
                {
                    // Déplacez le pion de l'IA vers la case voisine choisie
                    Button fromButton = boardButtons[aiPieceIndex];
                    Button toButton = boardButtons[neighbourIndex];
                    commentText.text = "AI is moving from " + fromButton + " to " + toButton;
                    MovePiece(fromButton, toButton.GetComponent<ClickableButton>());
                    aiPieceIndices.Remove(aiPieceIndex);
                    aiPieceIndices.Add(neighbourIndex);
                }

            }
            else
            {
                commentText.text = "Aucun pion de l'IA sur le plateau.";
            }

            // Change de tour pour le joueur
            isPlayersTurn = true;
        }

    }

    private int AiChooseOnePiece()
    {
        capture = AiHasPotentialCapture();
        foreach (int playerPlayerIndex in playerPieceIndices)
        {
            // Vérifier si le pion du joueur a des voisins (pions de l'IA)
            List<int> neighbourIndices = GetNeighbourIndices(playerPlayerIndex);

            foreach (int neighbourIndex in neighbourIndices)
            {
                List<int> neighbourNeighbours = GetAvailableNeighbourIndices(neighbourIndex);
                if (neighbourNeighbours.Count > 0)
                {
                    foreach (int neighbourNeighbourIndex in neighbourNeighbours)
                    {
                        if (IsOnSameRow(playerPlayerIndex, neighbourIndex, neighbourNeighbourIndex))
                        {
                            Button button = boardButtons[playerPlayerIndex];
                            Image image = button.GetComponent<Image>();
                            if (image != null)
                            {
                                image.sprite = null;
                                playerPieceIndices.Remove(playerPlayerIndex);
                                SetButtonInteractable(button, true);
                            }
                            return playerPlayerIndex;
                        }
                    }
                }
            }
            if (borderNumbers.Contains(playerPlayerIndex) && playerPlayerIndex != capture.neighbourCapturedIndex)
            {
                Button button = boardButtons[playerPlayerIndex];
                Image image = button.GetComponent<Image>();
                image.sprite = null;
                playerPieceIndices.Remove(playerPlayerIndex);
                SetButtonInteractable(button, true);
                return playerPlayerIndex;
            }
            if(playerPieceIndices.Count == 2)
            {
                if(playerPlayerIndex != capture.neighbourCapturedIndex)
                {
                    Button button = boardButtons[playerPlayerIndex];
                    Image image = button.GetComponent<Image>();
                    image.sprite = null;
                    playerPieceIndices.Remove(playerPlayerIndex);
                    SetButtonInteractable(button, true);
                    return playerPlayerIndex;
                }
            }
        }
        playerRestPions--;
        playerPiecesPlaced++;
        playerRestText.text = playerRestPions.ToString();   
        return 31;
    }


    // Fonction pour obtenir les indices des cases voisines disponibles pour un pion donné
    private List<int> GetAvailableNeighbourIndices(int index)
    {
        List<int> neighbourIndices = new List<int>();

        // Vérifiez les indices des cases voisines
        for (int i = 0; i < boardButtons.Length; i++)
        {
            if (AreNeighbours(index, i) && !playerPieceIndices.Contains(i) && !aiPieceIndices.Contains(i))
            {
                neighbourIndices.Add(i);
            }
        }

        return neighbourIndices;
    }

    private List<int> GetNeighbourIndicesAi(int index)
    {
        List<int> neighbourIndices = new List<int>();
        foreach (int neighbourIndex in aiPieceIndices) // Parcourir les indices des pions de l'adversaire
        {
            if (AreNeighbours(index, neighbourIndex))
            {
                neighbourIndices.Add(neighbourIndex);
            }
        }
        return neighbourIndices;
    }
    private List<int> GetNeighbourIndices(int index)
    {
        List<int> neighbourIndices = new List<int>();
        foreach (int neighbourIndex in playerPieceIndices) // Parcourir les indices des pions de l'adversaire
        {
            if (AreNeighbours(index, neighbourIndex))
            {
                neighbourIndices.Add(neighbourIndex);
            }
        }
        return neighbourIndices;
    }


    private bool IsOnSameRow(int index1, int index2, int index3)
    {
        // Vérifiez si les trois indices sont des voisins et s'ils sont alignés sur la même rangée ou colonne
        if ((AreNeighbours(index1, index2) && AreNeighbours(index2, index3)) && (Mathf.Abs(index1 - index2) == Mathf.Abs(index2 - index3)))
        {
            return true;
        }
        return false;
    }
    public struct Place
    {
        public int score;
        public int placeIndex;
    }
    public Place PlacePions()
    {
        Place place = new Place();
        List<int> pieces = new List<int>();
        List<int> emptyBoarderCases = new List<int>();
        List<int> borderNumbers = new List<int>() { 0,1,2,3,4,5,9,10,14,15,19,20,24,25,26,27,28,29 };
        pieces.AddRange(playerPieceIndices);
        pieces.AddRange(aiPieceIndices);
        foreach(int index in borderNumbers)
        {
            if (!pieces.Contains(index))
            {
                emptyBoarderCases.Add(index);
                place.score = +1000;
                place.placeIndex = index;
                return place;
            }
        }
        int randomIndex = Random.Range(0, boardButtons.Length);
        while (aiPieceIndices.Contains(randomIndex) || playerPieceIndices.Contains(randomIndex)) // Vérifie si l'indice a déjà été choisi par l'IA
        {
            randomIndex = Random.Range(0, boardButtons.Length);
        }
        place.score = +200;
        place.placeIndex = randomIndex;
        return place;
    }
    public struct Capture
    {
        public bool isPotentialCapture;
        public int aiPlayerIndex;
        public int neighbourIndex;
        public int neighbourCapturedIndex;
        public int score;
    }
    public Capture AiHasPotentialCapture()
    {
        Capture capture = new Capture();
        // Evaluation de l'etat de capture du joueur
        if (aiPieceIndices.Count > 0)
        {
            foreach (int aiPlayerIndex in aiPieceIndices)
            {
  
                // Vérifier si le pion du joueur a des voisins (pions de l'IA)
                List<int> neighbourIndices = GetNeighbourIndices(aiPlayerIndex);

                foreach (int neighbourIndex in neighbourIndices)
                {
                    List<int> neighbourNeighbours = GetAvailableNeighbourIndices(neighbourIndex);
                    if (neighbourNeighbours.Count > 0)
                    {
                        foreach (int neighbourNeighbourIndex in neighbourNeighbours)
                        {
                            if (IsOnSameRow(aiPlayerIndex, neighbourIndex, neighbourNeighbourIndex))
                            {
                                // Si ces conditions sont remplies, il y a un potentiel de capture
                                capture.isPotentialCapture = true;
                                capture.aiPlayerIndex = aiPlayerIndex;
                                capture.neighbourIndex = neighbourNeighbourIndex;
                                capture.neighbourCapturedIndex = neighbourIndex;
                                capture.score = +1000;
                                return capture;
                            }

                        }
                    }
                }
            }
        }
        // Evaluation de l'etat de fuite de l AI en cas d'éventuelle capture de l'AI
        if (playerPieceIndices.Count > 0)
        {
            foreach (int playerIndex in playerPieceIndices)
            {
                for (int i = 0; i < 30; i++)
                {
                    if (!aiPieceIndices.Contains(i) && !playerPieceIndices.Contains(i))
                    {
                        Button button1 = boardButtons[playerIndex];
                        ClickableButton button2 = boardButtons[i].GetComponentInParent<ClickableButton>();
                        if (IsOneCaseAdjacent(button1, button2))
                        {
                            List<int> neighbourPlayer = GetNeighbourIndicesAi(playerIndex);
                            List<int> neighbourI = GetNeighbourIndicesAi(i);
                            foreach (int neighbourIndex1 in neighbourPlayer)
                            {
                                foreach (int neighbourIndex2 in neighbourI)
                                {
                                    if (neighbourIndex1 == neighbourIndex2)
                                    {

                                        List<int> AiAvailableNeighbour = GetAvailableNeighbourIndices(neighbourIndex1);
                                        if (AiAvailableNeighbour.Count > 0)
                                        {
                                            foreach (int neighbourIndex3 in AiAvailableNeighbour)
                                            {
                                                List<int> playerNeighbour = GetNeighbourIndices(neighbourIndex3);
                                                if (playerNeighbour.Count == 0)
                                                {
                                                    capture.score = -1000;
                                                    capture.aiPlayerIndex = neighbourIndex1;
                                                    capture.neighbourIndex = neighbourIndex3;
                                                    return capture;
                                                }
                                            }

                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        // Evaluation de l'etat de déplacement de l'AI vers une case sécurisée
        if (aiPieceIndices.Count > 0)
        {
            foreach (int aiIndex in aiPieceIndices)
            {
                List<int> neighbours = GetAvailableNeighbourIndices(aiIndex);
                if (neighbours.Count > 0)
                {
                    foreach (int neighbour in neighbours)
                    {
                        List<int> playerNeighbour = GetNeighbourIndices(neighbour);
                        foreach (int playerNeighbourIndex in playerPieceIndices)
                        {
                            if (!playerNeighbour.Contains(playerNeighbourIndex))
                            {
                                capture.score = +500;
                                capture.aiPlayerIndex = aiIndex;
                                capture.neighbourIndex = neighbour;
                                return capture;
                            }
                        }
                    }
                }

            }
        }
        if (aiPieceIndices.Count > 0)
        {
            int randomIndex = Random.Range(0, aiPieceIndices.Count);
            int aiPieceIndex = aiPieceIndices[randomIndex];

            // Recherchez les cases voisines disponibles pour déplacer le pion
            List<int> availableNeighbourIndices = GetAvailableNeighbourIndices(aiPieceIndex);

            while (availableNeighbourIndices.Count == 0)
            {
                randomIndex = Random.Range(0, aiPieceIndices.Count);
                aiPieceIndex = aiPieceIndices[randomIndex];
                availableNeighbourIndices = GetAvailableNeighbourIndices(aiPieceIndex);
            }
            capture.isPotentialCapture = false;
            capture.aiPlayerIndex = aiPieceIndex;
            int randomNeighbourIndex = Random.Range(0, availableNeighbourIndices.Count);
            randomNeighbourIndex = availableNeighbourIndices[randomNeighbourIndex];
            capture.neighbourIndex = randomNeighbourIndex;
        }
        
        return capture;
    }

}
public class ClickableButton : MonoBehaviour
{
    public int buttonIndex; // Index du bouton dans le tableau boardButtons
}

