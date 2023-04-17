using System.Collections.Generic;
using LSL;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

public class MetroManager : MonoBehaviour {
    #region Fields

    #region Singleton

    public static MetroManager Instance;

    #endregion

    #region LibLSL

    private liblsl.StreamOutlet markerStream;

    #endregion

    #region Set In Editor

    public uint numGamesToSpawn = 1;

    #endregion

    #region Privates

    private List<MetroGame> games = new List<MetroGame>();

    // Used for UI stuff. The game that the player is currently "selecting". I.E. what they can interact with, add to, change, etc.
    // todo: Decide how to select. Should it just be nearest game? Should there be some UI for it? ETC. For now just select first game.
    private MetroGame selectedGame = null;

    #endregion

    #region UIs

    public GameObject menuUI;
    public GameObject metroUI;
    public GameObject addTrainUI;
    public GameObject LController;
    TransportLineUI[] lineUIs;

    #endregion

    #region Public

    public Random random = new Random();

    #endregion

    #endregion


    // Start is called before the first frame update
    void Start() {
        gameObject.AddComponent<Server>();

        liblsl.StreamInfo inf =
            new liblsl.StreamInfo("EventMarker", "Markers", 1, 0, liblsl.channel_format_t.cf_string);
        markerStream = new liblsl.StreamOutlet(inf);


        // Spawn in the games.

        if (numGamesToSpawn <= 0) {
            Debug.LogError("No games set to spawn!");
        }

        for (uint i = 0; i < numGamesToSpawn; i++) {
            var newMetroGame = (new GameObject("Game " + games.Count)).AddComponent<MetroGame>();
            newMetroGame.gameId = i;
            games.Add(newMetroGame);
            newMetroGame.transform.position = GetGameLocation(newMetroGame.gameId);

            // todo: Change later so that we can switch between games we want to control.
            if (i == 0) {
                SelectGame(newMetroGame);
            }
        }

        lineUIs = metroUI.GetComponentsInChildren<TransportLineUI>(true);
    }

    // Get the transform of the game with a certain id. This should space them apart so that there is sufficient distance between each game.
    private Vector3 GetGameLocation(uint gameID) {
        float
            distanceBetweenGames =
                10.0f; //todo: This is arbitrary right now. Radius in which stations can spawn grows with time, so we need some way to deal with that eventually.

        // Using a grid pattern.
        // todo: Will not work if games instances are added while program is running rather than just at spawn.

        uint maxX = (uint)math.floor(math.sqrt(numGamesToSpawn));
        uint x = gameID % maxX;
        uint z = (uint)math.floor((float)gameID / maxX);
        return new Vector3(x * distanceBetweenGames, 0.0f, z * distanceBetweenGames);
    }

    // Refresh the UI. EX: When selected game is reset or when switching the selected game.
    private void RefreshUI() {
        foreach (var l in lineUIs) {
            l.SetLine(null);
        }

        metroUI.SetActive(!selectedGame.isGameover);

        for (int i = 0; i < selectedGame.lines.Count; i++) {
            lineUIs[i].SetLine(selectedGame.lines[i]);
        }

        if (addTrainUI) {
            addTrainUI.SetActive(!selectedGame.isGameover);
        }

    }

    // Only change selected game through this function, so that delegates are properly assigned.
    private void SelectGame(MetroGame game) {
        if (selectedGame) {
            selectedGame.uiUpdateDelegate -= RefreshUI;
            selectedGame.OnSelectionChange(false);
            selectedGame = null;
        }

        selectedGame = game;
        selectedGame.uiUpdateDelegate += RefreshUI;
        selectedGame.OnSelectionChange(true);
    }

    public static void SendEvent(string eventString) {
        string[] tempSample;
        tempSample = new string[] { eventString };
        Instance.markerStream.push_sample(tempSample);
    }

    // Update is called once per frame
    void Update() { }

    private void Awake() {
        if (Instance is null) Instance = this;
        else {
            Destroy(this);
            Debug.LogError("More than one MetroManager initialized!");
        }
    }

    public static void ResetGame(uint gameID) {
        GetGameWithID(gameID).ScheduleReset();
    }

    public static JSONObject SerializeGame(uint gameID) {
        return GetGameWithID(gameID).SerializeGameState();
    }

    private static MetroGame GetGameWithID(uint gameID) {
        return Instance.games.Find(game => game.gameId == gameID);
    }

    public static void QueueGameAction(MetroGame.MetroGameAction action, uint gameID) {
        GetGameWithID(gameID).QueueAction(action);
    }

    public static MetroGame GetSelectedGame() {
        return Instance.selectedGame;
    }


    // Starts every game simultaneously.
    public static void StartGames() {
        foreach (var metroGame in Instance.games) {
            metroGame.StartGame();
        }
    }


    #region Station Name Generation
    

    /// <summary>
    /// Generates a random station name for this station. The name is only guaranteed to be unique within the same game instance.
    /// If you need to access stations by a globally unique ID, use their GUID.
    /// <example>
    /// 16th Street, Tangerine Green North, Capri Street, 12th Street, etc.
    /// </example>
    ///
    /// <param name="gameID">The ID of the game that is checked for conflicting station names.</param>
    /// </summary>
    public string GenerateRandomStationName(uint gameID) {
        var newName = "";

        var currStationNames = new List<string>();
        foreach (var station in GetGameWithID(gameID).stations) {
            currStationNames.Add(station.stationName);
        }
        
        while (newName == "") {
            newName = GenerateSingleStationName();

            if (currStationNames.Contains(newName)) {
                Debug.LogWarning("Same name (" + newName + ") was generated as already used in game " + gameID + "!");
                newName = "";
            }
        }

        return newName;
    }
    
    #region Hardcoded generation strings.
    
    private static readonly string[] Nouns = new string[] {
        // birds
        "Tinamou", "Ostrich", "Kiwi", "Cassowary", "Guinea", "Quail", "Partridge", "Grouse", "Capercaillie", "Curassow",
        "Pheasant", "Peacock", "Swan", "Merganser", "Smew", "Pochard", "Shearwater", "Prion", "Petrel", "Albatross",
        "Flamingo", "Grebe", "Stork", "Heron", "Egret", "Ibis", "Gannet", "Shoebill", "Falcon", "Eagle", "Kestrel",
        "Merlin", "Harrier", "Bateleur", "Hawk", "Seriema", "Mesite", "Crane", "Avocet", "Snipe", "Sandpiper", "Tern",
        "Pratincole", "Dove", "Pigeon", "Lorikeet", "Parakeet", "Corella", "Macaw", "Amazon", "Owl", "Swift",
        "Hummingbird", "Hermit", "Kingfisher", "Woodpecker",
        // spices
        "Anise", "Angelica", "Annatto", "Avocado", "Aniseed", "Basil", "Barberry", "Bayleaf", "Borage", "Cardamom",
        "Caper", "Caraway", "Cayenne", "Celery", "Chicory", "Cicely", "Cinnamon", "Clove", "Coriander", "Cudweed",
        "Dill", "Fennel", "Garlic", "Ginger", "Hyssop", "Marigold", "Jasmine", "Juniper", "Lavender", "Lemon",
        "Calamint", "Lovage", "Mace", "Mastic", "Mint", "Musk", "Nigella", "Nutmeg", "Olida", "Orris", "Paracress",
        "Parsley", "Perilla", "Rosemary", "Rue", "Saffron", "Safflower", "Sage", "Savory", "Spikenard", "Woodruff",
        "Thyme", "Tarragon", "Wattleseed", "Willow", "Wintergreen", "Wormwood",
        // berries
        "Acai", "Gooseberry", "Baneberry", "Barberry", "Bittersweet", "Mulberry", "Cloudberry", "Currant", "Dewberry",
        "Grape", "Holly", "Ivy", "Juneberry", "Logan", "Mistletoe", "Persimmon", "Privet", "Salmonberry", "Tayberry",
        // vegetables
        "Borage", "Catsear", "Chickweed", "Collard", "Cress", "Dandelion", "Fiddlehead", "Chayote", "Squash", "Vanilla",
        "Courgette", "Daylily", "Blossom", "Lentil", "Tepary", "Yardlong", "Cardoon", "Lotus", "Scallion", "Shallot",
        "Burdock", "Cassava", "Turmeric", "Yam", "Arame", "Dulse",
        // trees
        "Alder", "Ash", "Birch", "Beech", "Cherry", "Blackthorn", "Elm", "Hawthorn", "Hazel", "Hornbeam", "Linden",
        "Poplar", "Oak", "Pine", "Maple", "Aspen", "Rowan", "Whitebeam", "Willow", "Yew", "Buckthorn", "Elder",
        "Spindle", "Sallow", "Osier", "Guelder", "Rose", "Wayfaring", "Spruce", "Chestnut", "Larch", "Fir", "Cypress",
        "Hemlock",
        // last names
        "Williams", "Jackson", "Jones", "Brown", "Anderson", "Taylor", "Moore", "Davis", "Harris", "Robinson", "Clark",
        "Allen", "Hall", "Hill", "Scott", "Evans", "Parker", "Reed", "Cooper", "Howard", "Gray", "Watson", "Price",
        "Bennett", "Sanders", "Patterson", "Hughes", "Murphy", "Bell", "King", "Wright", "Lewis", "Brooks", "Graham",
        "Gibson", "Kennedy", "Mason", "Hunt", "Black", "Grant", "Stone", "Knight", "Hudson", "Spencer", "Stephens",
        "Pierce", "Henry", "Stevens", "Tucker", "Myers", "Washington", "Butler", "Barnes", "Coleman", "Palmer",
        "Gardner", "Webb", "McCoy", "Jacobs", "Burton", "Richards", "Kelley", "Andrews", "Weaver", "Moreno", "Fuller",
        "Lynch", "Garrett", "Lyons", "Ramsay", "Bush", "Watts", "Bates", "Harmon", "Newton", "Edison", "Sutton",
        "Craig", "Lowe", "Quincy", "Monroe",
        // colors
        "Periwinkle", "Hickory", "Amaranth", "Crimson", "Arctic", "Aureolin", "Azure", "Auburn", "Amethyst", "Amber",
        "Fuchsia", "Beige", "Olive", "Leather", "Pigment", "Blizzard", "Lagoon", "Sapphire", "Cerulean", "Brunswick",
        "Buff", "Cadet", "Byzantine", "Cadmium", "Camel", "Capri", "Carolina", "Celadon", "Chestnut", "Citrine",
        "Claret", "Cocoa", "Cobalt", "Copper", "Coquelicot", "Coral", "Cordovan", "Corn", "Cornflower", "Cream", "Cyan",
        "Orchid", "Midnight", "Liver", "Imperial", "Tangerine", "Tan", "Taupe", "Desert", "Fawn", "Gainsboro", "Hansa",
        "Gunmetal", "Honeydew", "Heliotrope", "Harlequin", "Indigo", "Inchworm", "Ivory", "Keppel", "Kelly", "Laurel",
        "Lapis", "Linen", "Lion", "Limerick", "Lumber", "Magnolia", "Maize", "Mahogany", "Mauve", "Maroon", "Maya",
        "Mikado", "Myrtle", "Moccasin", "Moss", "Napier", "Navajo", "Ochre", "Onyx", "Papaya", "Pineapple", "Puce",
        "Quartz", "Queen", "Raisin", "Regalia", "Rhythm", "Rosewood", "Royal", "Russet", "Saddle", "Sepia", "Satin",
        "Sand", "Sinopia", "Silver", "Seashell", "Smoke", "Slate", "Steel", "Spring", "Straw", "Teal", "Tea", "Tulip",
        "Umber", "Volt", "Wheat", "Wenge", "Windsor", "Wisteria", "Wine", "Zinfandel",
        // months
        "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November",
        "December",
        // pleasant words
        "Prosperity", "Superiority", "Pictorial", "Dulcet", "Champagne", "Chandelier", "Elixir", "Leisure", "Lithe",
        "Ripple", "Pyrrhic", "Oceanic", "Atlantic", "Panacea", "Sobriquet", "Elysium", "Soliloquy", "Serendipity",
        "Velvet", "Silk", "Sunset", "Sunrise", "Dusk", "Breeze", "Blush", "Sterling", "Montgomery", "Winthrop",
        "Prospect", "Pacific", "Meadow", "Flushing", "Marine", "Pastel", "University", "College", "Graduate", "Castle",
        "Bridge", "Pavilion", "Broad", "Monsoon", "Snow", "Lafayette", "Lexington", "Madison", "Frost", "Mercy",
    };

    private static readonly string[] NounsExtras = new string[] {
        // birds
        "Tinamou", "Ostrich", "Kiwi", "Cassowary", "Guinea", "Quail", "Partridge", "Grouse", "Capercaillie", "Curassow",
        "Pheasant", "Peacock", "Swan", "Merganser", "Smew", "Pochard", "Shearwater", "Prion", "Petrel", "Albatross",
        "Flamingo", "Grebe", "Stork", "Heron", "Egret", "Ibis", "Gannet", "Shoebill", "Falcon", "Eagle", "Kestrel",
        "Merlin", "Harrier", "Bateleur", "Hawk", "Seriema", "Mesite", "Crane", "Avocet", "Snipe", "Sandpiper", "Tern",
        "Pratincole", "Dove", "Pigeon", "Lorikeet", "Parakeet", "Corella", "Macaw", "Amazon", "Owl", "Swift",
        "Hummingbird", "Hermit", "Kingfisher", "Woodpecker",
        // spices
        "Anise", "Angelica", "Annatto", "Avocado", "Aniseed", "Basil", "Barberry", "Bayleaf", "Borage", "Cardamom",
        "Caper", "Caraway", "Cayenne", "Celery", "Chicory", "Cicely", "Cinnamon", "Clove", "Coriander", "Cudweed",
        "Dill", "Fennel", "Garlic", "Ginger", "Hyssop", "Marigold", "Jasmine", "Juniper", "Lavender", "Lemon",
        "Calamint", "Lovage", "Mace", "Mastic", "Mint", "Musk", "Nigella", "Nutmeg", "Olida", "Orris", "Paracress",
        "Parsley", "Perilla", "Rosemary", "Rue", "Saffron", "Safflower", "Sage", "Savory", "Spikenard", "Woodruff",
        "Thyme", "Tarragon", "Wattleseed", "Willow", "Wintergreen", "Wormwood",
        // berries
        "Acai", "Gooseberry", "Baneberry", "Barberry", "Bittersweet", "Mulberry", "Cloudberry", "Currant", "Dewberry",
        "Grape", "Holly", "Ivy", "Juneberry", "Logan", "Mistletoe", "Persimmon", "Privet", "Salmonberry", "Tayberry",
        // vegetables
        "Borage", "Catsear", "Chickweed", "Collard", "Cress", "Dandelion", "Fiddlehead", "Chayote", "Squash", "Vanilla",
        "Courgette", "Daylily", "Blossom", "Lentil", "Tepary", "Yardlong", "Cardoon", "Lotus", "Scallion", "Shallot",
        "Burdock", "Cassava", "Turmeric", "Yam", "Arame", "Dulse",
        // trees
        "Alder", "Ash", "Birch", "Beech", "Cherry", "Blackthorn", "Elm", "Hawthorn", "Hazel", "Hornbeam", "Linden",
        "Poplar", "Oak", "Pine", "Maple", "Aspen", "Rowan", "Whitebeam", "Willow", "Yew", "Buckthorn", "Elder",
        "Spindle", "Sallow", "Osier", "Guelder", "Rose", "Wayfaring", "Spruce", "Chestnut", "Larch", "Fir", "Cypress",
        "Hemlock",
        // last names
        "Williams", "Jackson", "Jones", "Brown", "Anderson", "Taylor", "Moore", "Davis", "Harris", "Robinson", "Clark",
        "Allen", "Hall", "Hill", "Scott", "Evans", "Parker", "Reed", "Cooper", "Howard", "Gray", "Watson", "Price",
        "Bennett", "Sanders", "Patterson", "Hughes", "Murphy", "Bell", "King", "Wright", "Lewis", "Brooks", "Graham",
        "Gibson", "Kennedy", "Mason", "Hunt", "Black", "Grant", "Stone", "Knight", "Hudson", "Spencer", "Stephens",
        "Pierce", "Henry", "Stevens", "Tucker", "Myers", "Washington", "Butler", "Barnes", "Coleman", "Palmer",
        "Gardner", "Webb", "McCoy", "Jacobs", "Burton", "Richards", "Kelley", "Andrews", "Weaver", "Moreno", "Fuller",
        "Lynch", "Garrett", "Lyons", "Ramsay", "Bush", "Watts", "Bates", "Harmon", "Newton", "Edison", "Sutton",
        "Craig", "Lowe", "Quincy", "Monroe",
        // colors
        "Periwinkle", "Hickory", "Amaranth", "Crimson", "Arctic", "Aureolin", "Azure", "Auburn", "Amethyst", "Amber",
        "Fuchsia", "Beige", "Olive", "Leather", "Pigment", "Blizzard", "Lagoon", "Sapphire", "Cerulean", "Brunswick",
        "Buff", "Cadet", "Byzantine", "Cadmium", "Camel", "Capri", "Carolina", "Celadon", "Chestnut", "Citrine",
        "Claret", "Cocoa", "Cobalt", "Copper", "Coquelicot", "Coral", "Cordovan", "Corn", "Cornflower", "Cream", "Cyan",
        "Orchid", "Midnight", "Liver", "Imperial", "Tangerine", "Tan", "Taupe", "Desert", "Fawn", "Gainsboro", "Hansa",
        "Gunmetal", "Honeydew", "Heliotrope", "Harlequin", "Indigo", "Inchworm", "Ivory", "Keppel", "Kelly", "Laurel",
        "Lapis", "Linen", "Lion", "Limerick", "Lumber", "Magnolia", "Maize", "Mahogany", "Mauve", "Maroon", "Maya",
        "Mikado", "Myrtle", "Moccasin", "Moss", "Napier", "Navajo", "Ochre", "Onyx", "Papaya", "Pineapple", "Puce",
        "Quartz", "Queen", "Raisin", "Regalia", "Rhythm", "Rosewood", "Royal", "Russet", "Saddle", "Sepia", "Satin",
        "Sand", "Sinopia", "Silver", "Seashell", "Smoke", "Slate", "Steel", "Spring", "Straw", "Teal", "Tea", "Tulip",
        "Umber", "Volt", "Wheat", "Wenge", "Windsor", "Wisteria", "Wine", "Zinfandel",
        // months
        "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November",
        "December",
        // pleasant words
        "Prosperity", "Superiority", "Pictorial", "Dulcet", "Champagne", "Chandelier", "Elixir", "Leisure", "Lithe",
        "Ripple", "Pyrrhic", "Oceanic", "Atlantic", "Panacea", "Sobriquet", "Elysium", "Soliloquy", "Serendipity",
        "Velvet", "Silk", "Sunset", "Sunrise", "Dusk", "Breeze", "Blush", "Sterling", "Montgomery", "Winthrop",
        "Prospect", "Pacific", "Meadow", "Flushing", "Marine", "Pastel", "University", "College", "Graduate", "Castle",
        "Bridge", "Pavilion", "Broad", "Monsoon", "Snow", "Lafayette", "Lexington", "Madison", "Frost", "Mercy",
        // Extras
        "York", "Stockholm", "Helsinki", "Belfast", "Berlin", "Rome", "Bombay", "Sydney", "Melbourne", "Boston", "Vancouver", "Portland", "Tokyo", "London", "Cairo", "Athens", "Tuscany", "Montauk",
    };

    private static readonly string[] DistrictPrefixes = new string[] {
        // Urban
        "North", "South", "West", "East", "New", "Old", "Downtown", "Fort", "Lower", "Upper",
        // Rural
        "Mount",
        // Seaside
        "Port", "Cape", "Isle of", "Point",
    };

    private static readonly string[] DistrictSuffixes = new string[] {
        // Urban
        "Square", "Place", "Green", "Heights", "Hill", "Plaza", "Slope", "Park", "Terrace", "City", "Point", "Village",
        "Gardens", "Manor", "Walk",
        // Rural
        "Plains", "Meadow", "Farms", "Woods", "Forest", "Hills", "Ridge", "Hook", "Point", "Neck", "Falls",
        // Seaside
        "Bay", "Shore", "Beach", "Coast", "Harbour", "Sands", "Point", "Island", "Isle", "Wharf",
    };

    private static readonly string[] RoadPrefixes = new string[] {
        "West", "East", "North", "South", "NE", "NW", "SE", "SW", "New", "Old", "Great"
    };

    private static readonly string[] RoadSuffixes = new string[] {
        // Generic
        "Street", "Street", "Road", "Street", "Lane", "Drive", "Street", "Road", "Row", "Road", "Street", "Way",
        // Avenue
        "Avenue", "Boulevard", "Avenue", "Parkway", "High Street", "Avenue", "Turnpike", "Flyover", "Overpass", "Avenue", "Avenue", "Boulevard", "Parkway", "Avenue",
        // Dead End
        "Street", "Close", "Alley", "Street", "Court", "End", "Close", "Drive", "Lane", "Alley", "Street", "Drive",
        // Roundabout
        "Roundabout", "Circle", "Circus", "Roundabout", "Oval", "Rotary", "Round", "Roundabout",
    };

    private static readonly string[] StationDistrictEnder = new string[] {
        "",
        "North",
        "",
        "South",
        "",
        "Central",
        ""
    };
        
    private static readonly string[] StationPrefixes = new string[] {
        "Grand", "Central", "North", "South"
    };
    
    private static readonly string[] StationWords = new string[] {
        // merriam webster's word of the day
        "Stalwart", "Libertine", "Rococo", "Verdant", "Constellation",
        // other words
        "Conference", "Confluence", "Union", "Liberty", "Franchise", "Prosperity", "Chapel", "Garden", "Alexandria", "Imperial", "Empire", "Sovereign", "August", "Majesty", "Kings", "Lords", "Commerce", "Monument", "Cathedral", "Pavilion", "Chambers", "Prince", "Rockefeller", "Lafayette", "Palisade",
        // gemstones
        "Emerald", "Ruby", "Sapphire", "Garnet", "Jade", "Quartz", "Amethyst", "Topaz",
        // italy
        "Florence", "Rome", "Tuscany", "Venice", "Napoli", "Torino", "Verona",
        // saints
        "St. Augustine", "St. Clementine", "St. Christopher", "St. Francis", "St. John", "St. Nicholas", "Saints"
    };
    
    private static readonly string[] StationSuffixes = new string[] {
        "Cross", "Junction", "Station", "Cross", "Circus", "Square", "Junction", "Plaza", "Terminal", "Hall"
    };

    
    #endregion

    // These simply generate names for different things using the Hardcoded strings above in certain ways.
    // Logic for each of these is adapted from the javascript in this website: https://groenroos.co.uk/names/
    #region Specialized Name Generators

    /// <summary>
    /// Internal helper used by GenerateRandomStationName().
    /// Logic adapted from the javascript in this website: https://groenroos.co.uk/names/
    /// </summary>
    /// <returns></returns>
    private string GenerateSingleStationName() {
        var station = "";

        if (random.NextDouble() > 0.5) {
            if (random.NextDouble() > 0.5) {
                station = GenerateSingleRoadName();
            }
            else {
                station = GenerateSingleDistrictName() + " " + StationDistrictEnder[random.Next(0, StationDistrictEnder.Length)];
            }
        }
        else {
            if (random.NextDouble() > 0.5) {
                station = StationPrefixes[random.Next(0, StationPrefixes.Length)];
            }
            station += " " + StationWords[random.Next(0, StationWords.Length)] + " " + StationSuffixes[random.Next(0, StationSuffixes.Length)];
        }

        return station;
    }

    /// <summary>
    /// Internal helper used by GenerateSingleStationName().
    /// Logic adapted from the javascript in this website: https://groenroos.co.uk/names/
    /// </summary>
    /// <returns></returns>
    private string GenerateSingleDistrictName () {
        if (random.NextDouble() > 0.75) {
            return Nouns[random.Next(0, Nouns.Length)] + " " + DistrictSuffixes[random.Next(0, DistrictSuffixes.Length)];
        }
        else {
            return DistrictPrefixes[random.Next(0, DistrictPrefixes.Length)] + " " +
                       Nouns[random.Next(0, Nouns.Length)];
        }
    }

    /// <summary>
    /// Internal helper used by GenerateSingleRoadName().
    /// Logic adapted from the javascript in this website: https://groenroos.co.uk/names/
    /// </summary>
    /// <returns></returns>
    private string ordinalSuffixOf(int i) {
        var j = i % 10;
        var k = i % 100;

        if (j == 1 && k != 11) {
            return i + "st";
        }

        if (j == 2 && k != 12) {
            return i + "nd";
        }

        if (j == 3 && k != 13) {
            return i + "rd";
        }

        return i + "th";
    }
    
    /// <summary>
    /// Internal helper used by GenerateSingleStationName().
    /// Logic adapted from the javascript in this website: https://groenroos.co.uk/names/
    /// </summary>
    /// <returns></returns>
    private string GenerateSingleRoadName() {
        var street = "";

        // Chance of prefix
        if (random.NextDouble() > 0.65) {
            street = RoadPrefixes[random.Next(0, RoadPrefixes.Length)];
        }
        
        // Chance of numbered
        if (random.NextDouble() > 0.85) {
            street += " " + ordinalSuffixOf(random.Next(1, 25));
        }
        else {
            street += " " + NounsExtras[random.Next(0, NounsExtras.Length)];
        }
        
        // Random ending
        street += " " + RoadSuffixes[random.Next(0, RoadSuffixes.Length)];

        return street;
    }
    

    #endregion
    
    #endregion
    
}
