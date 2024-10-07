import pandas as pd
import pyxdf
import cv2
import json
import sys
import os

args = sys.argv 
if(len(args) != 3):
    print("Error: Usage")
    print("Usage: \"py Preprocess.py (path to data folder) (path to video file)\"")
    print("Example: \"py Preprocess.py ./data ./data/screen_capture.mkv \"")
    exit()

dataFolder = args[1]
video_file = args[2]

# Position Offsets for occulusion view window in video:
video_offset_estimate = 1
xMin = 73 
xMax = 1846 
yMin = 42 
yMax = 1039 

screenWidth = int((xMax - xMin) / 2.0)
screenHeight = int((yMax - yMin))
xMid = xMin + screenWidth

# input filename
csv_file = os.path.join(dataFolder, 'EyetrackingScreenPosition.csv')  # Replace with your CSV file path

#output filenames:
left_file = 'left_coordinates.csv'
right_file = 'right_coordinates.csv'

#Find sync event in replay file:
print("Preprocessing input csv")
print("\tFinding sync time in replay file: ")
syncTimeLogs = -1

#{"TIME":19.86809,"GAME_ID":0,"EVENT_TYPE":"SyncGamesEvent","EVENT":null}
with open(os.path.join(dataFolder, 'game.replay'), 'r') as file:
    for line in file.readlines():
        data = json.loads(line)
        if( not 'EVENT_TYPE' in data):
            continue
        if(data['EVENT_TYPE'] == 'SyncGamesEvent'):
            syncTimeLogs = data['TIME']
            break

if(syncTimeLogs == -1):
    print("ERROR: No sync event found in replay file...")
    exit()

print("\tFound replay file sync time: " + str(syncTimeLogs))

# Start Generating transformed CSVs
#read input screenpositions
print("\tReading input csv")
data = pd.read_csv(csv_file)
#update timestamp by offset:

#update eye coordinates:
#leftX,leftY,rightX,rightY,timestamp

print("\tTransforming normalized coordinates")
data['leftX'] = data['leftX'] * screenWidth
data['leftY'] = (1-data['leftY']) * screenHeight
data['rightX'] = data['rightX'] * screenWidth
data['rightY'] = (1-data['rightY']) * screenHeight

print("\tCentering timestamps around sync event")
data['timestamp'] = data['timestamp'] - syncTimeLogs 

print("\tfiltering negative timestamps")
filtered_data = data[data['timestamp'] >= 0]
filtered_data = filtered_data.dropna()
end_time_eyetracking = filtered_data['timestamp'].iloc[-1]
end_time = end_time_eyetracking


# Find sync event in the video
print("Openning input video...")
cap = cv2.VideoCapture(video_file)
if not cap.isOpened():
    print("Error: Could not open video.")
    exit()
fps = cap.get(cv2.CAP_PROP_FPS)

#seek to end to calc duration
cap.set(cv2.CAP_PROP_POS_AVI_RATIO, 1)
last_frame = cap.get(cv2.CAP_PROP_POS_FRAMES)
duration = last_frame / fps
#seek back to beginning
cap.set(cv2.CAP_PROP_POS_FRAMES, 0)





# Define the codec and create a VideoWriter object
print("Preparing output format...")
frame_width = screenWidth
frame_height = screenHeight
print(" Resolution = " + str(frame_width) + ", " +  str(frame_height))
fourcc = cv2.VideoWriter_fourcc(*'mp4v')  # Use 'XVID', 'MJPG', etc. as needed
left_output_path  = 'left.mp4'
right_output_path = 'right.mp4'
left_out =  cv2.VideoWriter(left_output_path, fourcc, fps, (frame_width, frame_height))
right_out = cv2.VideoWriter(right_output_path, fourcc, fps, (frame_width, frame_height))


print("Finding sync event timestamp in video")
syncTimeVideo = -1
while cap.isOpened():
    current_time = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000.0
    ret, frame = cap.read()
    if not ret:
        print("Error: Something went wrong processing video")
        exit()
    if(syncTimeVideo == -1):
        # Get the current timestamp (in seconds)
        pixel_color = frame[yMax - 20, xMid]
        b,g,r = pixel_color
        if(current_time > video_offset_estimate and not(b==255 and r==255 and g==255)):
            syncTimeVideo = current_time
            print("Video sync time: " + str(syncTimeVideo))
            endTimeVideo = duration - syncTimeVideo

            print("\tEnd timestamp eyetracking = " + str(end_time_eyetracking))
            print("\tEnd timestamp video = " + str(endTimeVideo))
            if(endTimeVideo < end_time_eyetracking):
                print("Video is shorter, cutting eyetracking data")
                end_time = endTimeVideo
            else:
                print("Eyetracking data is shorter, cutting video")
            print("Beginning to produce output video")
    else:
        if(current_time > end_time + syncTimeVideo):
            break
        left_out.write(frame[yMin:yMax, xMin:xMid])
        right_out.write(frame[yMin:yMax, xMid:xMax])

print("Saving output videos")
left_out.release()
right_out.release()
cap.release()

print("Writing output CSVs")
leftColumns = ['timestamp','leftX','leftY','leftPupilDiameter']
rightColumns = ['timestamp','rightX','rightY','rightPupilDiameter']
dataLeft = filtered_data[leftColumns]
dataRight = filtered_data[rightColumns]
dataLeft.to_csv(left_file, index = True)
dataRight.to_csv(right_file, index = False)
