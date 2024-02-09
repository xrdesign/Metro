import sys
import json


#First define classes that mimic the data stored in the different log types:

class GameStruct:
    def __init__(self, gameJson):
        self.score = gameJson['score']
        self.time  = gameJson['time']
        self.isPause = gameJson['isPause']
        self.isGameover = gameJson['isGameover']
        self.freeTrains = gameJson['freeTrains']
        self.agentInsertStation = gameJson['agent_insert_station']
        self.agentRemoveStation = gameJson['agent_remove_station']
        self.agentRemoveTrack   = gameJson['agent_remove_track']
        self.stationsInserted = gameJson["stationsInserted"]
        self.stationsRemoved  = gameJson["stationsRemoved"]
        self.linesRemoved     = gameJson["linesRemoved"]
        self.linesCreated     = gameJson["linesCreated"]
        self.trainsAdded      = gameJson["trainsAdded"]
        self.trainsRemoved    = gameJson["trainsRemoved"]

        self.agentActions = self.agentInsertStation + self.agentRemoveStation + self.agentRemoveTrack

class SimStruct:
    def __init__(self, gameJson):
        self.score = gameJson['score']
        self.passengersDelivered = gameJson['passengersDelivered']
        self.totalWaitTime = gameJson['totalWaitTime']
        self.totalTravelTime = gameJson['totalTravelTime']
        self.stationCount = gameJson['station_count']

if __name__ == "__main__":
    filename = sys.argv[1] #Get log file path as first commandline arg
    outputFilename = "output.csv" #constant output file
    file=open(filename,"r") #open log file for reading
    outputFile=open(outputFilename, "w") #open output file for writing
    rawData = file.read(); #read log file
    data = json.loads(rawData) #parse log file data into json object
    logs = data["time_steps"] #create an array of logsteps from json

    numLogs = len(logs) 
    i = 1
    if numLogs < 2:
        i = 0
        print("Can't parse log with less than 2 entries") # weird issue with sim logs

    #determine the type of log using naming convention difference between sim
    #and regular logs:
    isSim = not "games" in logs[0] 

    #store the type of struct to create based on whether it is sim data or not
    struct = SimStruct if isSim else GameStruct
    
    #output array
    parsedLogs = []
    for i in range(len(logs)):
        #nested output array
        parsedGames = []

        #get a list of games
        games = logs[i]["Scores"] if isSim else logs[i]["games"]
        numGames = len(games)
        for g in range(numGames):
            #create the designated struct from the json:
            game = struct(games[g])
            parsedGames.append(game)
        parsedLogs.append(parsedGames)

    #At this point the parsedLogs array contains entries for each logstep in the file
    #where each entry is another array containing entires of the corresponding struct
    #based on the logfile type
    #eg: parsedLogs[5][3].score = the score of the 3rd game at timestep 5

    #store output as a csv
    for i in range(len(logs)):
        games = parsedLogs[i]
        numGames = len(games)
        for g in range(len(games)):
            outputData = games[g].stationsInserted #modify .score depending on desired information
            #eg: outputData = games[g].stationCount to get number of stations
            #or  outputData = games[g].agentActions to get number of agentActions 
            outputFile.write(str(outputData))
            if(g != numGames-1):
                outputFile.write(",")

        outputFile.write("\n")

    print("Done.  Output saved to \"output.csv\"")


        


        


