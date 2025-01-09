from typing import Tuple
from abc import ABC, abstractmethod
import heapq
import math, random

class GeometryUtils:
    @staticmethod
    def dot(a: Tuple[float, float, float], b: Tuple[float, float, float]) -> float:
        """Calculate dot product of two 3D vectors."""
        return (a[0] * b[0]) + (a[1] * b[1]) + (a[2] * b[2])

    @staticmethod
    def vec_from_points(a: Tuple[float, float, float], b: Tuple[float, float, float]) -> Tuple[float, float, float]:
        """Calculate the vector from point a to point b."""
        return (b[0] - a[0], b[1] - a[1], b[2] - a[2])

    @staticmethod
    def distance_between_points(a: Tuple[float, float, float], b: Tuple[float, float, float]) -> float:
        """Calculate Euclidean distance between two 3D points."""
        vector = GeometryUtils.vec_from_points(a, b)
        return math.sqrt(GeometryUtils.dot(vector, vector))

class PathUtils:
    @staticmethod
    def generate_segments_from_paths(planned_paths):
        """
        Generate Segment objects based on planned paths.
        Args:
            planned_paths (list): List of planned paths, where each path is a list of Station objects.
        Returns:
            list: List of Segment objects.
        """
        segments = []
        for line_idx, path in enumerate(planned_paths):
            for i in range(len(path) - 1):
                a = path[i].id
                b = path[i + 1].id
                length = GeometryUtils.distance_between_points(path[i].pos, path[i + 1].pos)
                segment = Segment(line=line_idx, a=a, b=b, length=length, index=i)
                segments.append(segment)
        return segments

    @staticmethod
    def generate_adjacency_list(segments):
        """
        Generate an adjacency list from a list of Segment objects.
        Args:
            segments (list): List of Segment objects.
        Returns:
            dict: Adjacency list where keys are station IDs and values are lists of (neighbor ID, distance).
        """
        adjacency_list = {}
        for segment in segments:
            if segment.a not in adjacency_list:
                adjacency_list[segment.a] = []
            if segment.b not in adjacency_list:
                adjacency_list[segment.b] = []
            adjacency_list[segment.a].append((segment.b, segment.length))
            adjacency_list[segment.b].append((segment.a, segment.length))
        return adjacency_list

class StationCost:
    def __init__(self, station, cost):
        self.id = station.id
        self.cost = cost

    def __repr__(self):
        return f"StationCost(station_id={self.id}, cost={self.cost})"


class StationCostManager:
    def __init__(self):
        self.station_costs = []
        self.all_stations = []
        self.planned_paths = []
        self.path_finder = None

    def add_cost(self, station, cost):
        self.station_costs.append(StationCost(station, cost))

    def get_cost_by_id(self, station_id):
        for station_cost in self.station_costs:
            if station_cost.station_id == station_id:
                return station_cost.cost
        return None

    def update_info(self, all_stations, planned_paths):
        self.all_stations = all_stations
        self.planned_paths = planned_paths
        self.station_costs = []  # Reset costs
        self.path_finder = self._create_path_finder(all_stations, planned_paths)
        self.station_costs = self.path_finder.compute_all_station_costs()

    def get_line_cost(self, line):
        line_cost = 0
        for station_id in line:
            station_cost = self.get_station_cost_by_id(station_id)
            if station_cost is not None:
                line_cost += station_cost
            else:
                return None
        return line_cost

    def total_cost(self):
        if self.station_costs == []:
            return float('inf')
        return sum(station_cost.cost for station_cost in self.station_costs)

    def _create_path_finder(self, all_stations, planned_paths):
        # To be overridden by subclasses
        pass

    def __repr__(self):
        return f"StationCostManager(station_costs={self.station_costs})"

class PathFinder(ABC):
    def __init__(self, stations, segments=None, planned_paths=None):
        """
        Initialize the base PathFinder with station and segment or planned path information.
        Args:
            stations (list): List of Station objects.
            segments (list, optional): List of Segment objects. Defaults to None.
            planned_paths (list, optional): List of planned paths. Defaults to None.
        """
        self.stations = stations
        self.planned_paths = planned_paths
        if segments:
            self.adjacency_list = PathUtils.generate_adjacency_list(segments)
        elif planned_paths:
            self.segments = PathUtils.generate_segments_from_paths(planned_paths)
            self.adjacency_list = PathUtils.generate_adjacency_list(self.segments)
        else:
            self.adjacency_list = {}

    @abstractmethod
    def find_route(self, start_id, goal_id):
        """
        Abstract method to find the shortest route between two stations.
        Must be implemented by subclasses.
        Args:
            start_id (int): ID of the starting station.
            goal_id (int): ID of the target station.
        Returns:
            tuple: (route, cost)
        """
        pass

    def reconstruct_route(self, came_from, start_id, goal_id):
        """
        Reconstruct the route from the start station to the goal station.
        Args:
            came_from (dict): Dictionary tracking the previous station for each station.
            start_id (int): ID of the starting station.
            goal_id (int): ID of the target station.
        Returns:
            list: List of station IDs representing the route.
        """
        route = []
        current_id = goal_id
        while current_id is not None:
            route.append(current_id)
            current_id = came_from[current_id]
        route.reverse()
        return route

    def compute_all_station_costs(self):
        cost_to_start = self.compute_costs_to_start
        cost_to_destination = self.compute_costs_to_destination
        assert len(cost_to_start) == len(self.stations)
        assert len(cost_to_destination) == len(self.stations)
        station_costs = []
        for i, station in enumerate( self.stations):
            cost = cost_to_start[i].cost + cost_to_destination[i].cost
            station_costs.append(StationCost(station, cost))
        return station_costs

    def compute_costs_to_destination(self):
        station_costs = []
        existed_types = list({station.shape for station in self.stations})

        # total cost over all types of passengers after metro picking them up
        for station in self.stations:
            type_list = existed_types.copy()
            type_list.remove(station.shape)
            station_cost = 0
            for type in type_list:
                possible_dsts = self.get_stations_for_shape_type(type)
                min_cost = float('inf')
                for dst in possible_dsts:
                    _, cost = self.find_route(station.id, dst.id)
                    min_cost = min(min_cost, cost)
                station_cost += min_cost
            station_costs.append(StationCost(station, station_cost))
        return station_costs

    def compute_costs_to_start(self):
        station_costs = []
        existed_types = list({station.shape for station in self.stations})

        for station in self.stations:
            station_cost = 0
            for path in self.planned_paths:
                if station in path:
                    start = station.id
                    # calculate distance from end_x to starting station
                    cost_on_path = 0
                    x_to_start = 0
                    for i in range(len(path)-1):
                        if i == start:
                            continue
                        station_a = path[i].pos
                        station_b = path[i+1].pos
                        x_to_start += GeometryUtils.distance_between_points(station_a, station_b)
                        if i+1 == start:
                            continue
                    y_to_start = 0
                    for i in range(len(path)-1, 0, -1):
                        if i == start:
                            continue
                        station_a = path[i].pos
                        station_b = path[i-1].pos
                        y_to_start += GeometryUtils.distance_between_points(station_a, station_b)
                    # worst cost for each type is twice of the distance to reach farer end
                    cost_on_path = 2*max(x_to_start, y_to_start)
                    # the total worst cost depends on the number of existed types of passengers
                    cost_on_path = cost_on_path*len(existed_types)
                station_cost += cost_on_path
            station_costs.append(StationCost(station, station_cost))
        return station_costs


    def get_stations_for_shape_type(self, shape_type):
        stations =  [station for station in self.stations if station.shape == shape_type]
        random.shuffle(stations)
        return stations

    def get_cost_manager(self, print_data=False) -> StationCostManager:
        existed_types = []
        stations_cost = 0
        station_cost_manager = StationCostManager()
        # record the types, existed in the game.
        for station in self.stations:
            existed_types.append(station.shape)
        existed_types = list(set(existed_types))
        for station in self.stations:
            type_list = existed_types.copy()
            type_list.remove(station.shape)
            if print_data:
                print(station.id)
                print(station.shape)
                print(type_list)
            station_cost = 0
            for type in type_list:
                possible_dsts = self.get_stations_for_shape_type(shape_type=type)
                shortest_route = []
                min_cost = float('inf')
                for possible_dst in possible_dsts:
                    route, cost = self.find_route(start_id=station.id, goal_id=possible_dst.id)
                    if cost < min_cost :
                        shortest_route = route
                        min_cost = cost
                station_cost += min_cost

                if print_data:
                    print("destination type: ", type)
                    print("shortest route: ", shortest_route)
            if print_data:
                print("station_cost: ", station_cost)
                print("=============")
            station_cost_manager.add_cost(station=station, cost=station_cost)
            stations_cost += station_cost
        if print_data:
            print("stations_cost: ", stations_cost)

        return station_cost_manager


class DijkstraPathFinder(PathFinder):
    def find_route(self, start_id, goal_id):
        """
        Find the shortest route from start_id to goal_id using Dijkstra's algorithm.
        Args:
            start_id (int): ID of the starting station.
            goal_id (int): ID of the target station.
        Returns:
            tuple: (route, cost)
        """
        priority_queue = [(0, start_id)]
        costs = {station.id: float('inf') for station in self.stations}
        costs[start_id] = 0
        came_from = {station.id: None for station in self.stations}

        while priority_queue:
            current_cost, current_id = heapq.heappop(priority_queue)
            if current_id == goal_id:
                route = self.reconstruct_route(came_from, start_id, goal_id)
                return route, current_cost

            for neighbor_id, distance in self.adjacency_list.get(current_id, []):
                new_cost = current_cost + distance
                if new_cost < costs[neighbor_id]:
                    costs[neighbor_id] = new_cost
                    came_from[neighbor_id] = current_id
                    heapq.heappush(priority_queue, (new_cost, neighbor_id))

        return [], float('inf')

class AStarPathFinder(PathFinder):
    def find_route(self, start_id, goal_id):
        """
        Find the shortest route from start_id to goal_id using A* algorithm.
        Args:
            start_id (int): ID of the starting station.
            goal_id (int): ID of the target station.
        Returns:
            tuple: (route, cost)
        """
        priority_queue = [(0, start_id)]
        costs = {station.id: float('inf') for station in self.stations}
        costs[start_id] = 0
        came_from = {station.id: None for station in self.stations}
        heuristic = lambda station_id: GeometryUtils.distance_between_points(
            self.stations[station_id].pos, self.stations[goal_id].pos
        )

        while priority_queue:
            current_cost, current_id = heapq.heappop(priority_queue)
            if current_id == goal_id:
                route = self.reconstruct_route(came_from, start_id, goal_id)
                return route, current_cost

            for neighbor_id, distance in self.adjacency_list.get(current_id, []):
                new_cost = current_cost + distance
                if new_cost < costs[neighbor_id]:
                    costs[neighbor_id] = new_cost
                    came_from[neighbor_id] = current_id
                    f_score = new_cost + heuristic(neighbor_id)
                    heapq.heappush(priority_queue, (f_score, neighbor_id))

        return [], float('inf')

# Refactored Classes
class DijkstraCostManager(StationCostManager):
    def __init__(self, all_stations, planned_paths):
        super().__init__()
        self.update_info(all_stations, planned_paths)

    def _create_path_finder(self, all_stations, planned_paths):
        return DijkstraPathFinder(stations=all_stations, planned_paths=planned_paths)

class AStarCostManager(StationCostManager):
    def __init__(self, all_stations, planned_paths):
        super().__init__()
        self.update_info(all_stations, planned_paths)

    def _create_path_finder(self, all_stations, planned_paths):
        return AStarPathFinder(stations=all_stations, planned_paths=planned_paths)

class Segment:
    def __init__(self, line=0, a=0, b=1, length=0, index=0):
        self.l = line
        self.a = a
        self.b = b
        self.length = length
        self.index = index