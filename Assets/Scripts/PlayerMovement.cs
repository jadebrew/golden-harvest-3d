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

    Rigidbody _rigidbody;
    public float _thrust;
    public float _jumpSpeed;
    public float _rotationSpeed = 10f;

    public int wood_amount = 0;

    private NPC _target;
    private GameObject _targetObject;
    private Vector3 direction;

    public GameObject itemSlot;

    private bool _showMenu;

    public Animator mAnimator;

    public GameObject heldModel;
    public Item equippedItem;

    public GameObject textPanel;
    public List<GameObject> menuTabs;
    private int tabIndex;

    [SerializeField]
    public Dictionary<Item,int> _inventory = new Dictionary<Item,int>();

    public GameObject buttonPrefab;
    public GameObject inventoryContainer;
    private bool faceTarget = false;

    private void Start()
    {
        //get the right components from the player object
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        menuAction = InputSystem.actions.FindAction("Crouch");
        nextAction = InputSystem.actions.FindAction("Next");
        previousAction = InputSystem.actions.FindAction("Previous");
        _rigidbody = GetComponent<Rigidbody>();

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

        mAnimator.SetBool("walking", false);
        //see if moveAction isnt zero and add as force the players rigidbody
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            var bod = this;

            direction = new Vector3(moveValue.x,0,moveValue.y);
            RotateTowards(direction);


            _rigidbody.AddForce(bod.transform.forward * _thrust);
            mAnimator.SetBool("walking", true);
        }

        if((_target != null) && _target.CanSee())
        {
            textPanel.SetActive(true);
        } else {
            textPanel.SetActive(false);
        }

        if (faceTarget)
        {
            RotateTowards(_targetObject.transform.position - transform.position);
        }

        updateInventory();
    }

    IEnumerator UseDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        var node = _targetObject.GetComponent<Node>();
        if (node != null)
        {
            if (node.vulnerability == equippedItem.damageType)
            {
                node.health -= equippedItem.damage;
                faceTarget = false;
            }
        }
    }

    void Update()
    {
        if(jumpAction.WasPressedThisFrame())
        {
            if (equippedItem != null)
            {
                mAnimator.SetTrigger("sweep");
                if (_targetObject != null)
                {
                    faceTarget = true;
                    StartCoroutine(UseDelay(0.7f));
                }
            }

        }
        if(menuAction.WasPressedThisFrame())
        {
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
        tabIndex = tabIndex;
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

    void closeMenu()
    {
        foreach (GameObject menu in menuTabs)
        {
            menu.SetActive(false);
        }
    }

    public void Give(Item item)
    {
        if (!_inventory.ContainsKey(item))
        {
            _inventory.Add(item,0);
        }
        _inventory[item] += 5;
        wood_amount = _inventory[item];
    }

    public void SetTarget(NPC npc)
    {

            _target = npc;
            textPanel.transform.Find("Portrait").GetComponent<Image>().sprite = npc.person.portrait;
            textPanel.transform.Find("Body").GetComponent<TMP_Text>().text = npc.person.chatter;
            textPanel.transform.Find("Title").GetComponent<TMP_Text>().text = npc.person.longname;
    }

    public void SetTarget(GameObject target)
    {
        _targetObject = target;
    }
}
