using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The player character: movement, targeting and hitting entities, the inventory,
/// crafting and the in-game menu.
/// </summary>
public class PlayerController : MonoBehaviour
{
    private static readonly int WalkingParam = Animator.StringToHash("walking");
    private static readonly int SweepParam = Animator.StringToHash("sweep");

    // Time between starting the swing animation and the hit landing.
    private static readonly WaitForSeconds HitDelay = new(0.7f);

    public static PlayerController Instance { get; private set; }

    // ---------------------------------------------------------------- Inspector

    [Header("Movement")]
    [SerializeField] private float moveForce;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Default Gear")]
    [SerializeField] private Item equippedItem;
    [Tooltip("Parent the held model is spawned under.")]
    [SerializeField] private GameObject itemSlot;

    [Header("Menu References")]
    [Tooltip("Menu tabs, cycled with Next/Previous. One of them is the crafting panel.")]
    [SerializeField] private List<GameObject> menuTabs;
    [SerializeField] private GameObject craftingContainer;
    [SerializeField] private GameObject inventoryContainer;


    [Header("Inventory & Recipes")]
    [SerializeField] private Dictionary<Item, int> inventory = new();
    [SerializeField] private List<Recipe> knownRecipes;
    private List<Item> indexedItemList = new();
    private int selectedItemIndex = 0;

    [Header("Menu Assets")]
    [Tooltip("Button with 'Icon' and 'Label' children, used for inventory and recipe entries.")]
    [SerializeField] private GameObject buttonPrefab;
    [Tooltip("Icon for recipes whose output has no sprite.")]
    [SerializeField] private Sprite defaultIcon;

    private AuraType _currentAura = AuraType.None;

    // ---------------------------------------------------------------- Runtime state

    private InputAction _moveAction;
    private InputAction _useAction;
    private InputAction _menuAction;
    private InputAction _nextTabAction;
    private InputAction _previousTabAction;

    private Rigidbody _rigidbody;
    private Animator _animator;
    private Entity _target;
    private bool _faceTarget;
    private bool _isMenuOpen;
    private int _tabIndex;

    // ---------------------------------------------------------------- Unity lifecycle

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // Action names come from the input actions asset.
        _moveAction = InputSystem.actions.FindAction("Move");
        _useAction = InputSystem.actions.FindAction("Jump");
        _menuAction = InputSystem.actions.FindAction("Interact");
        _nextTabAction = InputSystem.actions.FindAction("Next");
        _previousTabAction = InputSystem.actions.FindAction("Previous");
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        CloseMenu();
        SpawnHeldModel();
        RefreshMenuUI();
    }

    private void Update()
    {
        if (_useAction.WasPressedThisFrame())
        {
            UseEquippedItem();
        }

        if (_menuAction.WasPressedThisFrame())
        {
            ToggleMenu();
            RefreshMenuUI();
        }

        if (_nextTabAction.WasPressedThisFrame())
        {
            if (_isMenuOpen)
            {
                ShowMenuTab(_tabIndex + 1);
            }
            else
            {
                selectedItemIndex++;
                RefreshEquipment();
            }

        }

        if (_previousTabAction.WasPressedThisFrame())
        {
            if (_isMenuOpen)
            {
                ShowMenuTab(_tabIndex - 1);
            }
            else
            {
                selectedItemIndex--;
                RefreshEquipment();
            }
        }
    }

    private void FixedUpdate()
    {
        Move();
        UpdateTarget();
    }

    // ---------------------------------------------------------------- Movement

    private void Move()
    {
        var input = _moveAction.ReadValue<Vector2>();
        var isMoving = input != Vector2.zero;
        _animator.SetBool(WalkingParam, isMoving);
        if (!isMoving)
        {
            return;
        }

        _rigidbody.AddForce(transform.forward * (input.y * moveForce));
        var turn = Quaternion.Euler(0, input.x * rotationSpeed * Time.fixedDeltaTime, 0);
        _rigidbody.MoveRotation(_rigidbody.rotation * turn);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction),
            rotationSpeed);
    }

    // ---------------------------------------------------------------- Targeting & combat

    /// <summary>Called by entities in range; the closest one becomes the target.</summary>
    public void SetTarget(Entity candidate)
    {
        if (_target == null)
        {
            _target = candidate;
            return;
        }

        var position = transform.position;
        var candidateDistance = (candidate.transform.position - position).sqrMagnitude;
        var currentDistance = (_target.transform.position - position).sqrMagnitude;
        if (candidateDistance < currentDistance)
        {
            _target = candidate;
        }
    }

    private void UpdateTarget()
    {
        // Drop targets that died or that we walked away from, so the next entity in range can take over.
        if (_target != null && !_target.IsInPlayerRange())
        {
            _target = null;
        }

        if (_faceTarget && _target != null)
        {
            RotateTowards(_target.transform.position - transform.position);
        }
    }

    private void RefreshEquipment()
    {
        if (selectedItemIndex<0)
        {
            selectedItemIndex = indexedItemList.Count-1;
        }
        if (selectedItemIndex>=indexedItemList.Count)
        {
            selectedItemIndex = 0;
        }
        equippedItem = indexedItemList[selectedItemIndex];
        SpawnHeldModel();
    }

    private void UseEquippedItem()
    {
        if (equippedItem == null)
        {
            return;
        }

        _animator.SetTrigger(SweepParam);
        if (_target != null)
        {
            _faceTarget = true;
            StartCoroutine(HitAfterDelay(_target));
        }
    }

    private IEnumerator HitAfterDelay(Entity entity)
    {
        yield return HitDelay;

        if (entity != null && entity.IsInPlayerRange() && entity.vulnerability == equippedItem.damageType)
        {
            entity.TakeDamage(equippedItem.damage);
        }

        _faceTarget = false;
    }

    private void SpawnHeldModel()
    {
        ClearChildren(itemSlot.transform);
        Instantiate(equippedItem.model, itemSlot.transform);
    }

    // ---------------------------------------------------------------- Inventory

    public void Give(Item item, int amount)
    {
        ChangeAmount(item, amount);
        RefreshMenuUI();
    }

    private void ChangeAmount(Item item, int delta)
    {
        inventory[item] = GetAmount(item) + delta;
    }

    private int GetAmount(Item item)
    {
        return inventory.TryGetValue(item, out var amount) ? amount : 0;
    }

    /// <summary>Rebuilds the inventory and recipe lists; call after the inventory changes.</summary>
    private void RefreshMenuUI()
    {
        RefreshInventoryUI();
        RefreshCraftingUI();
    }

    private void RefreshInventoryUI()
    {
        ClearChildren(inventoryContainer.transform);
        indexedItemList.Clear();
        foreach (var (item, amount) in inventory)
        {
            CreateButton(inventoryContainer.transform, item.sprite, amount.ToString());
            indexedItemList.Add(item);
        }


    }

    // ---------------------------------------------------------------- Crafting

    private void RefreshCraftingUI()
    {
        ClearChildren(craftingContainer.transform);
        foreach (var recipe in knownRecipes)
        {
            CreateRecipeButton(recipe);
        }
    }

    private void CreateRecipeButton(Recipe recipe)
    {
        var icon = defaultIcon;
        foreach (var outputItem in recipe.output.Keys)
        {
            icon = outputItem.sprite;
        }

        var label = new StringBuilder();
        var canCraft = true;
        foreach (var (item, amount) in recipe.input)
        {
            label.Append(amount).Append("x ").Append(item.itemName).Append('\n');
            if (GetAmount(item) < amount)
            {
                canCraft = false;
            }
        }

        if (_target == null)
        {
            _currentAura = AuraType.None;
        } else {
            _currentAura = _target.aura;
        }

        //only even show recipe when you have required aura
        if (recipe.requiredAura == _currentAura)
        {
            var button = CreateButton(craftingContainer.transform, icon, label.ToString()).GetComponent<Button>();
            button.onClick.AddListener(() => Craft(recipe));
            button.interactable = canCraft;
        }
    }

    private void Craft(Recipe recipe)
    {
        foreach (var (item, amount) in recipe.input)
        {
            ChangeAmount(item, -amount);
        }

        foreach (var (item, amount) in recipe.output)
        {
            ChangeAmount(item, amount);
        }

        RefreshMenuUI();
    }

    // ---------------------------------------------------------------- Menu

    private void ToggleMenu()
    {
        if (_isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            ShowMenuTab(0);
        }
    }

    private void ShowMenuTab(int index)
    {
        CloseMenu();

        // Wrap around in both directions.
        _tabIndex = (index % menuTabs.Count + menuTabs.Count) % menuTabs.Count;
        menuTabs[_tabIndex].SetActive(true);
        _isMenuOpen = true;
    }

    private void CloseMenu()
    {
        foreach (var tab in menuTabs)
        {
            tab.SetActive(false);
        }

        _isMenuOpen = false;
    }

    // ---------------------------------------------------------------- UI helpers

    private GameObject CreateButton(Transform parent, Sprite icon, string label)
    {
        var button = Instantiate(buttonPrefab, parent);
        button.transform.Find("Icon").GetComponent<Image>().sprite = icon;
        button.transform.Find("Label").GetComponent<TMP_Text>().text = label;
        return button;
    }

    private static void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
