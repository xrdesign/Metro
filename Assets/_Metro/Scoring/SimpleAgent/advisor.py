from path_finder_utils import GameHandler, AStarCostManager, StationCostManager
from MetroWrapper import Station, Line
from typing import List
import time, random, copy
from agent import insert_station

class Advisor:
    def __init__(self, all_stations: List[Station], lines: List[Line]):
        self.cost_manager: StationCostManager = AStarCostManager(all_stations=all_stations, lines=lines)
        self.current_planned_paths: List[List[Station]] = self.cost_manager.planned_paths
        self.future_planned_paths: List[List[Station]] = [path[:] for path in self.current_planned_paths]
        self.future_cost_manager: StationCostManager = AStarCostManager(all_stations=all_stations, lines=lines)
        self.unconnected_stations: List[Station] = self.infer_unconnected_stations()
        self.connected_stations: List[Station] = [station for station in self.cost_manager.all_stations if station not in self.unconnected_stations]

    def update_info_using_gamesinfo(self, all_stations: List[Station], lines: List[Line]):
        self.cost_manager.update_info_using_gamesinfo(all_stations=all_stations, lines=lines)
        self.current_planned_paths = self.cost_manager.planned_paths
        self.future_planned_paths: List[List[Station]] = [path[:] for path in self.current_planned_paths]
        self.future_cost_manager.update_info_using_plan(all_stations=all_stations, planned_paths=self.future_planned_paths)
        self.unconnected_stations = self.infer_unconnected_stations()
        self.connected_stations: List[Station] = [station for station in self.cost_manager.all_stations if station not in self.unconnected_stations]

    def infer_unconnected_stations(self):
        unconnected_stations: List[Station] = []
        for station in self.cost_manager.all_stations:
            not_in = True
            for planned_path in self.current_planned_paths:
                if station in planned_path:
                    not_in = False
                    break
            if not_in:
                unconnected_stations.append(station)
        return unconnected_stations

    def infer_next_connection(self) -> List[List[Station]]:
        raise NotImplementedError("Subclasses must implement infer_next_connection() to return a list of planned paths.")


class StochasticGreedyAdvisor(Advisor):
    def infer_next_connection(self, attempts: int = 1e4, whether_print = False, whether_implement:bool = False, game_handler: GameHandler=None) -> List[List[Station]]:
        best_candidate = None
        best_candidate_station = None
        best_chosen_path_index = None
        best_insert_position = None
        best_cost = self.future_cost_manager.total_cost()

        # Make sure we have at least one unconnected station to try.
        if not self.unconnected_stations:
            return self.future_planned_paths

        for _ in range(int(attempts)):
            # 1. Pick a random station from the unconnected stations.
            candidate_station: Station = random.choice(self.unconnected_stations)

            # 2. Clone current planned paths: create a shallow copy of each sublist.
            candidate_paths = [path[:] for path in self.future_planned_paths]

            # 3. Insert the candidate station into a random planned path.
            if candidate_paths:
                # Instead of using random.choice directly, determine the index of the chosen path.
                chosen_path_index = random.randint(0, len(candidate_paths) - 1)
                chosen_path = candidate_paths[chosen_path_index]

                # Pick a random insertion position in that chosen path.
                insert_position = random.randint(0, len(chosen_path))
                chosen_path.insert(insert_position, candidate_station)
            else:
                # If there are no existing planned paths, create a new one.
                candidate_paths.append([candidate_station])
                chosen_path_index = 0
                insert_position = 0

            # 4. Evaluate candidate cost.
            self.future_cost_manager.update_info_using_plan(all_stations=self.cost_manager.all_stations, planned_paths=candidate_paths)
            candidate_cost = self.future_cost_manager.total_cost()

            # 5. If this candidate has a lower cost than our current best, save it.
            if candidate_cost < best_cost or len(self.unconnected_stations)>1:
                best_cost = candidate_cost
                best_candidate = candidate_paths
                best_candidate_station = candidate_station
                best_chosen_path_index = chosen_path_index
                best_insert_position = insert_position


        if best_candidate is not None:
            self.future_planned_paths = best_candidate
            if whether_print:
                print(f"Suggest to insert station {best_candidate_station.id} to the position {best_insert_position} on path {best_chosen_path_index}.")
                print(f"Following the suggestion, the total cost will be {best_cost}")
            if whether_implement and game_handler:
                insert_station(ws=game_handler.ws, line=best_chosen_path_index, station=best_candidate_station.id, insert=best_insert_position, game_id=game_handler.game_id)
                self.update_info_using_gamesinfo(all_stations=game_handler.stations, lines=game_handler.lines)
            else:
                self.future_cost_manager.update_info_using_plan(all_stations=self.cost_manager.all_stations, planned_paths=self.future_planned_paths)
                if whether_print:
                    print(f"The total cost of all stations is {self.cost_manager.total_cost()}, and will be {self.future_cost_manager.total_cost()}")


        # If a candidate was found, return it; otherwise, return the current paths.
        if best_candidate is None:
            return self.future_planned_paths, None, None, None
        # Return the best candidate paths, the station to insert, the path index, and the insertion position.
        return self.future_planned_paths, best_candidate_station.id, best_insert_position, best_chosen_path_index


if __name__ == "__main__":
    game_count = 1
    game_address = 'ws://localhost:3000/metro'
    game_handlers: List[GameHandler] = [GameHandler(game_id=i, game_address=game_address) for i in range(game_count)]
    advisors = [StochasticGreedyAdvisor(all_stations=game_handlers[i].stations, lines=game_handlers[i].lines) for i in range(game_count)]

    while True:
        for i in range(game_count):
            whether_update = game_handlers[i].update_gamestate()
            if whether_update:
                advisors[i].update_info_using_gamesinfo(all_stations=game_handlers[i].stations, lines=game_handlers[i].lines)
                # game_handlers[i].send_station_costs_to_game(advisors[i].cost_manager)
            if len(advisors[i].unconnected_stations)>0:
                _, best_candidate_station_id, best_insert_postion, best_chosen_path_index = advisors[i].infer_next_connection(attempts=5e2, whether_print=True, whether_implement=False, game_handler=game_handlers[i])
                game_handlers[i].send_recommendation_to_game(best_candidate_station_id, best_insert_postion, best_chosen_path_index)
        time.sleep(1)