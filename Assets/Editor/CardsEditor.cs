using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class CardsEditor : EditorWindow
{
    private VisualTreeAsset visualTreeAsset;

    [MenuItem("Cards/Cards Editor")]
    public static void ShowCardsEditor()
    {
        CardsEditor wnd = GetWindow<CardsEditor>();
        wnd.titleContent = new GUIContent("CardsEditor");
        wnd.minSize = new Vector2(900, 500);
    }

    public void CreateGUI()
    {
        // Add the layout and styling to the window
        visualTreeAsset = 
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/CardsEditor.uxml");
        visualTreeAsset.CloneTree(rootVisualElement);

        // The first time the window gets opened, nothing will be selected
        // Therefore clear the right side
        HideRightColumn();

        StyleSheet styleSheet =
            AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CardsEditor.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        // Add functionality to the buttons / when selection changes
        RegisterButtonCallbacks();

        // Populate the list view with all available cards
        DrawCardsListView();
    }

    private void RegisterButtonCallbacks()
    {
        ToolbarButton saveChangesButton = rootVisualElement.Q<ToolbarButton>("save-button");
        saveChangesButton.clicked += SaveAllChanges;

        Button createCardButton = rootVisualElement.Q<Button>("create-card-button");

        ToolbarMenu addRemoveMenu = rootVisualElement.Q<ToolbarMenu>("add-remove-menu");
        addRemoveMenu.menu.AppendAction("Create new card", evt => { CreateNewCard(); });
        addRemoveMenu.menu.AppendAction("Delete this card", evt => { DeleteCard(); });

        ToolbarSearchField searchBar = rootVisualElement.Q<ToolbarSearchField>("name-search-field");
        searchBar.RegisterValueChangedCallback(evt =>
        {
            DrawCardsListView(searchBar.value);
        });
    }

    private void DrawCardsListView(string nameToSearch = null)
    {
        // First find all the cards
        Card[] allCards = GetAllCards(nameToSearch);

        // If the array is empty, return
        if (allCards.Length == 0) return;

        // Now find the listview, and add all cards to it
        ListView cardsList = rootVisualElement.Q<ListView>("cards-list");

        cardsList.makeItem = () => new Label();
        cardsList.bindItem = (element, i) => (element as Label).text = (i >= allCards.Length) ? string.Empty : allCards[i].name;
        cardsList.itemsSource = allCards;
        cardsList.itemHeight = 20;
        cardsList.selectionType = SelectionType.Single;

        // When a new element gets selected, handle this
        cardsList.onSelectionChange += (enumberable) =>
        {
            // Redraw the right column
            DrawRightColumn();

            // Get the newly selected card
            Card selectedCard = cardsList.selectedItem as Card;

            // Change the image
            LoadCardImage(selectedCard.CardSprite);

            SerializedObject serializedObject = new SerializedObject(selectedCard);

            // Bind the image field to the object
            ObjectField imageField = rootVisualElement.Q<ObjectField>("card-image-field");
            imageField.BindProperty(serializedObject.FindProperty("cardSprite"));
            imageField.RegisterValueChangedCallback(evt =>
            {
                LoadCardImage(selectedCard.CardSprite);
            });

            // Bind the name field
            TextField cardNameField = rootVisualElement.Q<TextField>("card-name-field");
            cardNameField.BindProperty(serializedObject.FindProperty("m_Name"));
            cardNameField.RegisterValueChangedCallback(evt =>
            {
                if (!string.IsNullOrEmpty(cardNameField.value))
                {
                    serializedObject.ApplyModifiedProperties();
                    cardsList.Refresh();
                }
            });

            // Bind the description field
            TextField descriptionField = rootVisualElement.Q<TextField>("card-description-field");
            descriptionField.BindProperty(serializedObject.FindProperty("description"));

            // Bind the integer fields
            IntegerField attackField = rootVisualElement.Q<IntegerField>("attack-value");
            IntegerField defenseField = rootVisualElement.Q<IntegerField>("defense-value");
            IntegerField manacostField = rootVisualElement.Q<IntegerField>("manacost-value");

            attackField.BindProperty(serializedObject.FindProperty("attackValue"));
            defenseField.BindProperty(serializedObject.FindProperty("defenseValue"));
            manacostField.BindProperty(serializedObject.FindProperty("manaCost"));

            // Bind the enum field
            EnumField tierField = rootVisualElement.Q<EnumField>("card-tier");
            tierField.BindProperty(serializedObject.FindProperty("cardTier"));
        };
    }

    private void HideRightColumn()
    {
        VisualElement rightColumn = rootVisualElement.Q<VisualElement>("right-column");
        rightColumn.Clear();
    }

    private void DrawRightColumn()
    {
        VisualElement rightColumn = rootVisualElement.Q<VisualElement>("right-column");
        rightColumn.Clear();

        VisualElement tempElement = new VisualElement();
        visualTreeAsset.CloneTree(tempElement);

        VisualElement statsElement = tempElement.Q<VisualElement>("card-stats");
        VisualElement imageElement = tempElement.Q<VisualElement>("card-image-container");

        rightColumn.Add(statsElement);
        rightColumn.Add(imageElement);
    }

    private void HideListView()
    {
        VisualElement leftColumn = rootVisualElement.Q<VisualElement>("left-column");
        leftColumn.Clear();
    }

    private void RedrawListView()
    {
        VisualElement leftColumn = rootVisualElement.Q<VisualElement>("left-column");
        leftColumn.Clear();

        VisualElement tempElement = new VisualElement();
        visualTreeAsset.CloneTree(tempElement);

        ListView listView = tempElement.Q<ListView>("cards-list");
        leftColumn.Add(listView);

        DrawCardsListView();
    }

    private Card[] GetAllCards(string nameToSearch = null)
    {
        // Finds all the cards in the project, the t: parameter lets us search for types
        string filter = string.Empty;

        if (!string.IsNullOrEmpty(nameToSearch))
        {
            filter = $"{nameToSearch} t:Card";
        }
        else
        {
            filter = "t:Card";
        }

        string[] guids = AssetDatabase.FindAssets(filter);

        // Create and populate the array
        Card[] allCards = new Card[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            allCards[i] = AssetDatabase.LoadAssetAtPath<Card>(path);
        }

        return allCards;
    }

    private void LoadCardImage(Sprite cardSprite)
    {
        VisualElement cardImage = rootVisualElement.Q<VisualElement>("card-image");
        cardImage.style.backgroundImage = new StyleBackground(cardSprite);
    }

    private void CreateNewCard()
    {
        // First save the changes to the cards, else the files won't be renamed
        SaveAllChanges();

        // Figure out how many cards already exist and use it to name the new card
        int totalCards = GetAllCards().Length;
        string newCardName = $"Card {++totalCards}";

        // Create the object
        Card newCard = ScriptableObject.CreateInstance<Card>();
        AssetDatabase.CreateAsset(newCard, $"Assets/Cards/{newCardName}.asset");
        AssetDatabase.SaveAssets();

        // Redraw the list view
        DrawCardsListView();

        // Make the new card the selected object
        Selection.activeObject = newCard;
    }

    private void DeleteCard()
    {
        // First find out which card is currently selected (if any)
        ListView cardsList = rootVisualElement.Q<ListView>("cards-list");
        if (cardsList.selectedItem == null) return;

        Card cardToDelete = cardsList.selectedItem as Card;

        // First clear the selection
        HideRightColumn();
        HideListView();

        string path = AssetDatabase.GetAssetPath(cardToDelete);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();

        RedrawListView();
    }

    private void SaveAllChanges()
    {
        Card[] allCards = GetAllCards();

        foreach (Card c in allCards)
        {
            if (c == null) continue;
            string cardName = c.name;
            string path = AssetDatabase.GetAssetPath(c);

            AssetDatabase.RenameAsset(path, cardName);
        }

        AssetDatabase.SaveAssets();
    }
}