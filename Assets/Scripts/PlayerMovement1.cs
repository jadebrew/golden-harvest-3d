using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    InputAction moveAction;
    InputAction jumpAction;
    InputAction menuAction;
    InputAction nextAction;
    InputAction previousAction;
    InputAction interactAction;

    Rigidbody _rigidbody;
    public float _thrust;
    public float _jumpSpeed;
    public float _rotationSpeed = 10f;

    [Header("Default Gear")]
    public GameObject itemSlot;
    private bool _showMenu;
    private Animator _animator;
    public GameObject heldModel;
    public Item equippedItem;

    [Header("Menu References")]
    public GameObject craftingContainer;
    public GameObject craftingPanel;
    public GameObject inventoryContainer;
    public List<GameObject> menuTabs;
    private int tabIndex;

    [Header("Inventory & Recipes")]
    [SerializeField]
    public Dictionary<Item,int> _inventory = new Dictionary<Item,int>();
    public List<Recipe> knownRecipes;
    private bool inMenu = true;

    [Header("Prefabs")]
    public GameObject buttonPrefab;
    public Sprite defaultIcon;
    private bool faceTarget = false;

    [Header("Private References")]
    private GameObject _targetObject;
    private Vector3 direction;
    private Dictionary<AuraType,float> environmentBuffs = new Dictionary<AuraType,float>();


    private void Start()
    {
        //get the right components from the player object
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        menuAction = InputSystem.actions.FindAction("Crouch");
        nextAction = InputSystem.actions.FindAction("Next");
        previousAction = InputSystem.actions.FindAction("Previous");
        interactAction = InputSystem.actions.FindAction("Interact");
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        closeMenu();
        Equip();
    }

    void Equip()
    {
        var container = itemSlot;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }
        Instantiate(heldModel,itemSlot.transform);
    }

    void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));

        var bod = this;

        bod.transform.rotation = Quaternion.RotateTowards(
            bod.transform.rotation,
            targetRotation,
            _rotationSpeed
        );
    }

    void FixedUpdate()
    {

        _animator.SetBool("walking", false);
        //see if moveAction isnt zero and add as force the players rigidbody
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            var bod = this;
            _rigidbody.AddForce(bod.transform.forward * moveValue.y * _thrust);
            _animator.SetBool("walking", true);
            Vector3 _AngleVelocity = new Vector3(0, moveValue.x*_rotationSpeed, 0);
            Quaternion deltaRotation = Quaternion.Euler(_AngleVelocity * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }

        if (_targetObject != null && faceTarget)
        {
            RotateTowards(_targetObject.transform.position - transform.position);
        }
        updateInventory();
    }

    IEnumerator UseDelay(float delayTime)
    {
        var entity = _targetObject.GetComponent<Entity>();
        yield return new WaitForSeconds(delayTime);

        if (entity != null)
        {
            if (entity.InRange() && entity.vulnerability == equippedItem.damageType)
            {
                entity.health -= equippedItem.damage;

            }
            else {
                faceTarget = false;
            }
        }
        faceTarget = false;
    }

    void Update()
    {
        if(jumpAction.WasPressedThisFrame())
        {
            if (equippedItem != null)
            {
                _animator.SetTrigger("sweep");
                if (_targetObject != null)
                {
                    faceTarget = true;
                    StartCoroutine(UseDelay(0.7f));
                }
            }

        }


        if(interactAction.WasPressedThisFrame())
        {
            showCraftMenu();
            if (!_showMenu)
            {
                showMenu(0);
                _showMenu = true;
            } else {
                closeMenu();
                _showMenu = false;
            }

        }
        if(nextAction.WasPressedThisFrame())
        {
            if (_showMenu)
            {
                tabIndex++;
                showMenu(tabIndex);
            }
        }
        if(previousAction.WasPressedThisFrame())
        {
            if (_showMenu)
            {
                tabIndex--;
                showMenu(tabIndex);
            }
        }

    }

    void showMenu(int index)
    {
        tabIndex = index;
        closeMenu();
        if (tabIndex>=menuTabs.Count)
        {
            tabIndex = 0;
        } else if (tabIndex < 0)
        {
            tabIndex = menuTabs.Count-1;
        }
        updateInventory();
        menuTabs[tabIndex].SetActive(true);
    }

    void updateInventory()
    {
        var container = inventoryContainer;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }

        foreach(KeyValuePair<Item,int> item in _inventory)
        {
            var button = Instantiate(buttonPrefab,container.transform);
            button.transform.Find("Icon").GetComponent<Image>().sprite = item.Key.sprite;
            button.transform.Find("Label").GetComponent<TMP_Text>().text = item.Value.ToString();
        }
    }

    void showCraftMenu()
    {
        //craftingPanel.SetActive(true);
        //_showMenu = true;

        var container = craftingContainer;
        foreach(Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Recipe _recipe in knownRecipes)
        {

            var text = "";
            Sprite icon = defaultIcon;
            bool CanCraft = true;
            foreach (KeyValuePair<Item,int> _item in _recipe.output)
            {
                //text = text + _item.Key.itemName;
                icon =  _item.Key.sprite;

            }
            foreach (KeyValuePair<Item,int> _item in _recipe.input)
            {
                text = text + _item.Value + "x " + _item.Key.itemName + "\n";
                if (!_inventory.ContainsKey(_item.Key))
                {
                    CanCraft = false;
                } else {
                    if(_inventory[_item.Key] < _item.Value)
                    {
                        CanCraft = false;
                    }
                }
            }
            var button = Instantiate(buttonPrefab,container.transform);
            button.transform.Find("Icon").GetComponent<Image>().sprite = icon;
            button.transform.Find("Label").GetComponent<TMP_Text>().text = text;
            button.GetComponent<Button>().onClick.AddListener(() => CraftItem(_recipe));
            if(!CanCraft)
            {
                 button.GetComponent<Button>().interactable = false;
            }
        }
    }

    public void CraftItem(Recipe recipe)
    {
        foreach (KeyValuePair<Item,int> _item in recipe.input)
        {
           Remove(_item.Key,_item.Value);
        }
        foreach (KeyValuePair<Item,int> _item in recipe.output)
        {
            Give(_item.Key,_item.Value);
        }
        showCraftMenu();
    }

    void closeMenu()
    {
        foreach (GameObject menu in menuTabs)
        {
            menu.SetActive(false);
        }
        craftingPanel.SetActive(false);
        _showMenu = false;
    }

    public void Give(Item item, int amount)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] += amount;
    }

    public void Remove(Item item, int amount)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] -= amount;
    }

    public void SetTarget(GameObject target)
    {
        float distance = Vector3.Distance(this.transform.position,target.transform.position);
        float previousDistance = 999;
        if (_targetObject != null)
        {
            previousDistance = Vector3.Distance(this.transform.position,_targetObject.transform.position);
        }
        if (distance < previousDistance)
        {
            _targetObject = target;
        }
    }
}
