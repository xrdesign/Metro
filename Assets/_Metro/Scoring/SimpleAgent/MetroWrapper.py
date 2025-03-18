
import sys
import math
import json
import heapq
from typing import List

SHAPES = ["Sphere", "Cone", "Cube"]
PositiveInfinity = 100000000

def GetDistance(a, b):
    c0 = b[0] - a[0]
    d = c0 * c0
    c1 = b[1] - a[1]
    d += c1 * c1
    c2 = b[2] - a[2]
    d += c2 * c2
    return math.sqrt(d)

class GameState:
    def __init__(self, gameJson):
        #create stations:
        self.stations: List[Station] = []
        self.stationMappings = {}
        stationsData = gameJson['stations']
        self.score = gameJson['score']
        self.time = gameJson['time']
        for stationData in stationsData:
            station = Station(stationData)
            self.stations.append(station)
            self.stationMappings[stationData['unique_id']] = station.id

        self.lines: List[Line] = []
        self.lineMappings = {}
        linesData = gameJson['lines']
        for lineData in linesData:
            line = Line(lineData)
            self.lines.append(line)
            self.lineMappings[line.uuid] = line.id

        self.UpdateSegments()
        self.UpdateNeighbors()
        """
        self.segments = []
        segmentsData = gameJson['segments']
        for segmentData in segmentsData:
            segment = Segment(segmentData, self.lineMappings, self.stationMappings)
            a = segment.a
            b = segment.b
            l = segment.l
            d = GetDistance(self.stations[a].pos, self.stations[b].pos)
            segment.length = d
            segment.index = len(self.lines[l].segments)
            self.segments.append(segment)
            self.stations[a].neighbors.append((b,l,d))
            self.stations[b].neighbors.append((a,l,d))
            self.lines[l].segments.append(segment)
            self.lines[l].totalLength += segment.length
        """
    def ClearLines(self):
        for l in range(len(self.lines)):
            self.lines[l].totalLength = 0
            self.lines[l].segments = []
        for s in range(len(self.stations)):
            self.stations[s].neighbors = []
        self.segments = []

    def CreateLine(self, route, lineIdx):
        if(len(route) < 2):
            return
        i = 0
        j = 1
        while j < len(route):
            a = route[i]
            b = route[j]
            d = GetDistance(self.stations[a].pos, self.stations[b].pos)
            segment = Segment(line = lineIdx, a = a, b = b, length = d)

            a = segment.a
            b = segment.b
            l = segment.l
            d = GetDistance(self.stations[a].pos, self.stations[b].pos)
            segment.length = d
            segment.index = len(self.lines[l].segments)
            self.segments.append(segment)
            self.stations[a].neighbors.append((b,l,d))
            self.stations[b].neighbors.append((a,l,d))
            self.lines[l].segments.append(segment)
            self.lines[l].totalLength += segment.length

            i+=1
            j+=1

    def InsertStation(self, stationIndex, lineIndex, insertIndex):
        self.lines[lineIndex].stops.insert(insertIndex, stationIndex)

    def UpdateSegments(self):
        self.segments = []
        for l, line in enumerate(self.lines):
            line.segments = []
            line.totalLength = 0
            i = 0
            j = 1
            if(len(line.stops) <= 1):
                continue
            while j < len(line.stops):
                a = line.stops[i]
                b = line.stops[j]
                d = GetDistance(self.stations[a].pos, self.stations[b].pos)

                segment = Segment(line = l, a = line.stops[i], b = line.stops[j], length = d, index = i)
                self.segments.append(segment)
                line.segments.append(segment)
                line.totalLength += d

                i += 1
                j += 1

    def UpdateNeighbors(self):
        for station in self.stations:
            station.neighbors = []
        for segment in self.segments:
            a = segment.a
            b = segment.b
            l = segment.l
            d = segment.length
            self.stations[a].neighbors.append((b,l,d))
            self.stations[b].neighbors.append((a,l,d))

    def Evaluate(self):
        totalCrowdingRate = 0
        avgWaitTime = 0
        for station in self.stations:
            stationWaitTime = 0
            for shape in SHAPES:
                if shape == station.shape:
                    continue
                waitTime = self.CalculateWaitTime(station, shape)
                #passengerSpawnRate = .075 per in game hour
                #in game day = 20 seconds -> ingame hour = 20seconds/24 = .833
                #passengersPerSecond = .075 / .833 = .09003
                #secondsPerPassenger = 11.107
                #secondsPerPassengerOfType = (types - 1)*11.107
                stationWaitTime += waitTime
                #print(f"waitTime: {waitTime}, score{currScore}")
            avgStationWaitTime = stationWaitTime/2
            avgWaitTime += avgStationWaitTime
            stationCrowdingRate = .09003 - (1/avgStationWaitTime)
            totalCrowdingRate += stationCrowdingRate
        avgWaitTime /= len(self.stations)
        totalCrowdingRate = .09003 - (1/avgWaitTime)
        return totalCrowdingRate

    def CalculateWaitTime(self, station, shape):
        route, fScore = self.FindRoute(station, lambda x : x.shape == shape)

        if len(route) <= 0:
            return PositiveInfinity
        else:
            i = 0
            j = 1
            lineID = -1
            distOnCurrLine = 0
            waitTime = 0
            while j < len(route):
                current = self.stations[route[i]]
                next = self.stations[route[j]]
                for neighbor in current.neighbors:
                    if(neighbor[0] != next.id):
                        continue
                    if(lineID == -1):
                        lineID = neighbor[1]
                    elif(neighbor[1] != lineID):
                        #transfer
                        totalDistance = self.lines[lineID].totalLength
                        worstCase = totalDistance * 2 + distOnCurrLine
                        bestCase = distOnCurrLine
                        avg = (worstCase + bestCase) / 2
                        waitTime += avg
                        distOnCurrLine = 0
                    lineID = neighbor[1]
                    distOnCurrLine += neighbor[2]
                    break
                i+=1
                j+=1
            totalDistance = self.lines[lineID].totalLength
            worstCase = totalDistance * 2 + distOnCurrLine
            bestCase = distOnCurrLine
            avg = (worstCase + bestCase) / 2
            waitTime += avg
            return waitTime
    def ReconstructRoute(self, start, end, cameFrom):
        route = []
        current = end
        while(current != start):
            route.append(current.id)
            current = self.stations[cameFrom[current.id]]
        route.append(start.id)
        route.reverse()
        return route

    def FindRoute(self, start, criteria):
        # use A * to find a shortest route, if no route found, find the route to
        # the closest station to the target
        route = []
        closedSet = []
        # openSet is a sorted List with fScore as priority, lowest fScore is the
        # first elementopenSet
        openSet = []
        cameFrom = {}
        gScore = {}
        fScore = {}

        gScore[start.id] = 0
        fScore[start.id] = 1#HeuristicCostEstimate(start, start); TODO

        heapq.heappush(openSet, (fScore[start.id], start.id));

        while len(openSet) > 0:
            # get first station in the openSet
            currentID = heapq.heappop(openSet)[1]
            current = self.stations[currentID]
            if criteria(current):
               # if the station is the goal, reconstruct the route
               route = self.ReconstructRoute(start, current, cameFrom);
               break

            closedSet.append(current.id);
            # for all neighbor of the current station
            # Find all neighboring stations
            neighbors = current.neighbors
            for neighborPair in neighbors:
                neighbor = self.stations[neighborPair[0]]
                if neighbor.id in closedSet:
                    continue
                tentative_gScore = gScore[current.id] + GetDistance(current.pos, neighbor.pos)
                neighborGScore = PositiveInfinity
                if neighbor in gScore:
                    neighborGScore = gScore[neighbor.id]
                if tentative_gScore < neighborGScore:
                    cameFrom[neighbor.id] = current.id
                    gScore[neighbor.id] = tentative_gScore
                    fScore[neighbor.id] = gScore[neighbor.id] + 1#HeuristicCostEstimate
                found = False
                for pair in openSet:
                    if pair[1] == neighbor.id:
                        found = True
                        break
                if not found:
                    heapq.heappush(openSet, (fScore[neighbor.id], neighbor.id))
        return (route, fScore)


    def Print(self):
        print("Stations: ")
        for i, station in enumerate(self.stations):
            print(f"Station: {i}")
            station.Print()
            print("")
        print("Lines: ")
        for i, line in enumerate(self.lines):
            print(f"Line: {i}")
            line.Print()
            print("")
        for i, segment in enumerate(self.segments):
            print(f"Segment: {i}")
            segment.Print()
            print("")

class Station:
    def __init__(self, stationJson):
        self.id = stationJson['id']
        self.shape = stationJson['shape']
        self.pos = (stationJson['x'], stationJson['y'], stationJson['z'])
        self.neighbors = [] # (targetStation, lineID, distance)
    def Print(self):
        print(self.id)
        print(self.shape)
        print(self.pos)
        print("Neighbors:")
        for edge in self.neighbors:
            print(f"\tstation: {edge[0]}, line: {edge[1]}, distance: {edge[2]}")


class Line:
    def __init__(self, lineJson):
        self.id = lineJson['id']
        self.uuid = lineJson['unique_id']
        self.totalLength = 0
        self.segments = []
        self.stops = []
        for stop in lineJson['stops']:
            self.stops.append(stop['id'])
    def Print(self):
        print(f"id: {self.id}")
        print(f"uuid: {self.uuid}")
        print(f"length: {self.totalLength}")

class Segment:
    def __init__(self, segmentJson = None, lineMappings = None, stationMappings = None, line = 0, a = 0, b = 1, length = 0, index = 0):
        if not segmentJson:
            self.l = line
            self.a = a
            self.b = b
            self.length = length
            self.index = index
        else:
            self.l = lineMappings[segmentJson['which_line']]
            self.a = stationMappings[segmentJson['from_station']]
            self.b = stationMappings[segmentJson['to_station']]
            self.length = segmentJson['length']
            self.index = 0
    def Print(self):
        print(f"l: {self.l}")
        print(f"index: {self.index}")
        print(f"from: {self.a}")
        print(f"to: {self.b}")
