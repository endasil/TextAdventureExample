namespace TextAdventureExample;


// Huvudklassen som håller hela spelet igång.
class Program
{
    // Lista av giltiga riktningar en spelare kan gå
    static readonly List<string> _validDirections = new List<string>
        {"north", "south", "east", "west", "north east", "south east", "south west", "north west"};

    // Databas över alla rum
    static Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
    // Spelarens inventory
    static List<GameObject> _inventory = new List<GameObject>();
    // Det nuvarande rummet där spelaren befinner sig
    static int _currentRoom = 0;


    static void Main(string[] args)
    {
        // Initialiserar spelet
        InitializeGame();
        // Visar hjälpkommandon
        HelpCommand();
        // Beskriver det nuvarande rummet
        LookCommand();
        // Huvudloop
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine("What will you do?");
            var command = Console.ReadLine();
            
            ParseCommand(command.Split(' '));
        }
    }

    static void InitializeGame()
    {

        // Skapar nycklar och dörrar
        var bronzeKey = new Key("Bronze Key", "An old bronze key.");
        var goldKey = new Key("Gold Key", "A shiny gold key.");

        var caveDoor = new Door("Cave Door", "A sturdy metallic door with no key hole.", true);
        var treasureDoor = new Door("Gold Door", "A lavishly decorated door made of pure gold.", true);

        // Sätter upp att treasureDoor ska kunna låsas upp av en nyckel som har namnet "Gold Key"
        treasureDoor.CanInteractWithItems.Add(goldKey.Name);

        // Skapar första rummet
        var cave = new Room
        {
            Description = "You are in a dark, dank cave. A mysterious door without a keyhole can be seen."
        };

        // sätter upp en dörr som måste låsas upp om användaren vill gå österut
        cave.AddDoor(caveDoor, "east"); // Using AddDoor to add the door

        var treasureRoom = new Room
        {
            Description =
                "You've entered the treasure room! Awe-inspiring piles of gold and jewels surround you. A majestic golden door catches your eye."
        };
        treasureRoom.AddDoor(treasureDoor, "north"); 

        var sanctum = new Room
        {
            Description =
                "You enter a serene sanctum. The atmosphere is peaceful, almost magical. Ancient artifacts are placed on pedestals, glowing faintly."
        };

        // Lägger till alla rum som ska finns
        _rooms.Add(cave.RoomId, cave);
        _rooms.Add(treasureRoom.RoomId, treasureRoom);
        _rooms.Add(sanctum.RoomId, sanctum);

        // Länkar upp vart man kommer när man går i olika riktningar från olika rum
        cave.Neighbors["east"] = treasureRoom;
        treasureRoom.Neighbors["west"] = cave;
        treasureRoom.Neighbors["north"] = sanctum;
        sanctum.Neighbors["south"] = treasureRoom;

        var lever = new Lever("Lever", "A mysterious Lever, perhaps you can use it?");
        
        // Om man skriver use lever så ska funktioen Unlock() på caveDoor köras.
        lever.OnUseFunctions += caveDoor.Unlock;

        // lägger till en bronz nyckel i grottan
        cave.Objects.Add(bronzeKey);
        // lägger till en spak i grottan
        cave.Objects.Add(lever);
        // 
        cave.Objects.Add(goldKey);
        
        //  Sätter upp att guldnynckeln kan användas på dörren I skattkammaren
        treasureDoor.CanInteractWithItems.Add(goldKey.Name);

        // Sätter upp att vi ska börja i grottan.
        _currentRoom = cave.RoomId;
    }

    // Analyserar inmatat kommando och utför motsvarande funktion
    static void ParseCommand(string[] parameters)
    {
        string command = parameters[0].ToLower();
        if (IsDirection(command))
        {
            GoCommand(parameters);
            return;
        }

        switch (command)
        {
            // existing cases
            case "inventory":
                InventoryCommand();
                break;
            case "use":
                UseCommand(parameters[1..]); // ..1 pass everything after the first word
                break;
            case "look":
                LookCommand(parameters[1..]);
                break;
            case "take":
                TakeCommand(parameters[1..]);
                break;
            case "help":
                HelpCommand();
                break;
            default:
                Console.WriteLine("Invalid command.");
                break;
        }
    }

    // Funktion för att lista alla möjliga kommandon
    static void HelpCommand()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine("look                                  - Give a description of the current room, all objects in it and available exists.");
        Console.WriteLine("look [item name]                      - Describes a specific item.");
        Console.WriteLine("take [item name]                      - Takes an item.");
        Console.WriteLine("use [item name]                       - Uses an item.");
        Console.WriteLine("use [item name] on [target name]      - Uses an item on something.");
        Console.WriteLine("inventory                             - Shows your current inventory.");
        Console.WriteLine("help                                  - Shows this help message.");
        Console.WriteLine($"To move, use: {string.Join(", ", _validDirections)} if available under exits.");
        // Add more commands as you implement them.
    }

    static void TakeCommand(string[] parameters)
    {
        // Re-join the parameters to consider multi-word names
        string itemName = string.Join(" ", parameters);

        var roomItem = _rooms[_currentRoom].Objects
            .Find(obj => obj.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (roomItem == null)
        {
            Console.WriteLine($"There is no item called {itemName} in the room.");
            return;
        }

        if (roomItem.CanBePickedUp)
        {
            _inventory.Add(roomItem);
            _rooms[_currentRoom].Objects.Remove(roomItem);
            Console.WriteLine($"You picked up {itemName}.");
        }
        else
        {
            Console.WriteLine("You can't pick that up.");
        }
    }

    static bool IsDirection(string direction)
    {
        return _validDirections.Contains(direction);
    }

    static void GoCommand(string[] parameters)
    {
        string direction = string.Join(" ", parameters);
        if (!_rooms[_currentRoom].Neighbors.ContainsKey(direction))
        {
            Console.WriteLine($"You cannot go {direction}.");
            return;
        }

        Door door = _rooms[_currentRoom].FindDoor(direction);
        if (door != null && door.IsLocked)
        {
            Console.WriteLine($"The door to the {direction} is locked.");
            return;
        }

        _currentRoom = _rooms[_currentRoom].Neighbors[direction].RoomId;
        Console.WriteLine($"You move {direction}.");
        LookCommand();
    }

    private static void InventoryCommand()
    {
        Console.WriteLine("You are carrying:");
        foreach (var item in _inventory)
        {
            Console.WriteLine(item.Name);
        }
    }

    static void UseCommand(string[] parameters)
    {
        // Hitta index för ordet "on" i parametrarna
        int indexOfOn = Array.IndexOf(parameters, "on");

        // Om "on" inte finns, eller är det första eller sista ordet i kommandot,
        // antar vi att användaren vill använda ett föremål utan ett specifikt mål.
        if (indexOfOn == -1 || indexOfOn == 0 || indexOfOn == parameters.Length - 1)
        {
            // Samla ihop alla ord för att bilda namnet på föremålet som ska användas
            string nameOfItemToUse = string.Join(" ", parameters);

            // Sök efter föremålet både i inventory och i rummet
            var itemToUseInInventory =
                _inventory.Find(obj => obj.Name.Equals(nameOfItemToUse, StringComparison.OrdinalIgnoreCase));
            var itemToUseInRoom = _rooms[_currentRoom].Objects
                .Find(obj => obj.Name.Equals(nameOfItemToUse, StringComparison.OrdinalIgnoreCase));

            // Om föremålet hittas i inventory, använd det
            if (itemToUseInInventory != null)
            {
                // Om inga funktioner har lagts till att köras när föremålet används, informera användaren
                if (itemToUseInInventory.OnUseFunctions == null)
                {
                    Console.WriteLine($"You can't use {nameOfItemToUse}.");
                    return;
                }
                // Om vi hittar föremålet i vår ryggsäck (inventory), anropa alla funktioner som lags till 
                // i OnUseFunctions, eller om OnUseFunctions är null, gör ingenting.
                itemToUseInInventory.OnUseFunctions?.Invoke();
            }

            if (itemToUseInRoom != null)
            {
                if (itemToUseInRoom.OnUseFunctions == null)
                {
                    Console.WriteLine($"You can't use {nameOfItemToUse}.");
                    return;
                }

                itemToUseInRoom.OnUseFunctions?.Invoke();
            }

            // Om föremålet inte hittas varken i rummet eller i inventariet, informera användaren
            if (itemToUseInInventory == null && itemToUseInRoom == null)
                Console.WriteLine($"There is no item called {nameOfItemToUse} in the room or in your inventory.");

            return;
        }

        // Om "on" finns i kommandot, dela upp det i två delar: föremålet och målet
        string itemName = string.Join(" ", parameters, 0, indexOfOn);
        string targetName = string.Join(" ", parameters, indexOfOn + 1, parameters.Length - indexOfOn - 1);

        // Sök efter föremålet i inventariet
        var inventoryItem = _inventory.Find(obj => obj.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        // Sök efter målet i rummet
        var roomItem = _rooms[_currentRoom].Objects
            .Find(obj => obj.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        
        // Om föremålet inte finns i inventariet, informera användaren.
        if (inventoryItem == null)
        {
            Console.WriteLine($"There is no item in your inventory called {itemName}");
            return;
        }

        if (roomItem == null)
        {
            Console.WriteLine($"There is no item in the room called {targetName}.");
            return;
        }

        // Kontrollera om föremålet och målet kan interagera med varandra
        if (inventoryItem.CanInteract(roomItem) || roomItem.CanInteract(inventoryItem))
        {
            roomItem.InteractWith(inventoryItem);
            Console.WriteLine($"You used {itemName} on {targetName}.");
        }
        else
        {
            Console.WriteLine("Those items can't be used together.");
        }
    }

    static void LookCommand(string[] parameters = null)
    {
        if (parameters == null || parameters.Length == 0)
        {
            Console.WriteLine("");
            Console.WriteLine(_rooms[_currentRoom].Description);
            Console.WriteLine("You see:");
            foreach (var obj in _rooms[_currentRoom].Objects)
            {

                Console.WriteLine(obj.Name);
            }

            Console.WriteLine("Exits:");
            foreach (var direction in _rooms[_currentRoom].Neighbors.Keys)
            {
                var door = _rooms[_currentRoom].FindDoor(direction);
                if (door?.IsLocked == true)
                {
                    continue; // Skip this direction if the door is locked
                }

                Console.WriteLine(direction);
            }
        }
        else
        {
            string objectName = string.Join(" ", parameters);
            var obj = _rooms[_currentRoom].Objects
                .Find(o => o.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
            if (obj != null)
            {
                Console.WriteLine(obj.Description);
            }
            else
            {
                Console.WriteLine($"There is no object called {objectName} in the room.");
            }
        }
    }
}
public class GameObject
{
    public GameObject(string name, string description)
    {
        Name = name;
        Description = description;
    }

    // Tänk på "OnUseFunctions" som en låda där vi kan lägga in 
    // olika uppdrag (funktioner) som vi vill att föremålet ska utföra när det används.
    // Till exempel kan ett uppdrag vara att låsa upp en dörr eller tända en lampa.

    public delegate void UseEffect();
    public UseEffect OnUseFunctions;
    public string Description { get; set; }

    public string Name { get; set; }
    public List<string> CanInteractWithItems { get; set; } = new List<string>();
    public bool CanBePickedUp { get; set; } = true;
    public virtual bool CanInteract(GameObject target)
    {
        return CanInteractWithItems.Contains(target.Name);
    }
    public virtual void InteractWith(GameObject target)
    {
    }
}

public class Door : GameObject
{
    public bool IsLocked { get; set; }


    public void Unlock()
    {
        this.IsLocked = false;
        Console.WriteLine($"The {this.Name} has been unlocked!");
    }


    public override void InteractWith(GameObject target)
    {
        if (target is not Key key) return;

        if (!this.CanInteractWithItems.Contains(key.Name) || !this.IsLocked) return;
        Unlock();
    }

    public Door(string name, string description, bool isLocked) : base(name, description)
    {
        IsLocked = isLocked;
        CanBePickedUp = false;
    }
}
public class Lever : GameObject
{
    public void AddOnUseFunction(UseEffect functionToCallWhenUsed)
    {
        this.OnUseFunctions += functionToCallWhenUsed;
    }

    public Lever(string name, string description) : base(name, description)
    {
        CanBePickedUp = false;
    }
}

public class Key : GameObject
{
    public Key(string name, string description) : base(name, description)
    {
    }
}


public class Room
{
    private static int _lastAssignedId = 0;

    public int RoomId { get; private set; }
    public string Description { get; set; }
    public List<GameObject> Objects { get; set; } = new List<GameObject>();
    public Dictionary<string, Room> Neighbors { get; } = new Dictionary<string, Room>();
    private Dictionary<string, Door> Doors { get; } = new Dictionary<string, Door>();

    public Room()
    {
        RoomId = ++_lastAssignedId;
    }

    public void AddDoor(Door door, string direction)
    {
        Objects.Add(door);
        Doors.Add(direction, door);
    }

    public Door FindDoor(string direction)
    {
        if (Doors.TryGetValue(direction, out var door))
        {
            return door;
        }
        return null;
    }
}

