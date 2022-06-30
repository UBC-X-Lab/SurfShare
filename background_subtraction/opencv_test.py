#from __future__ import print_function
import cv2 as cv
import argparse
import numpy as np

parser = argparse.ArgumentParser(description='This program shows how to use background subtraction methods provided by \
                                              OpenCV. You can process both videos and images.')
parser.add_argument('--input', type=str, help='Path to a video or a sequence of image.', default='test_images/real_test1.mp4')
parser.add_argument('--algo', type=str, help='Background subtraction method (KNN, MOG2).', default='MOG2')
args = parser.parse_args()

## [create]
#create Background Subtractor objects
if args.algo == 'MOG2':
    backSub = cv.createBackgroundSubtractorMOG2(detectShadows=False, history=1000)
elif args.algo == 'KNN':
    backSub = cv.createBackgroundSubtractorKNN()
elif args.algo == "GMG":
    backSub = cv.bgsegm.createBackgroundSubtractorGMG()
elif args.algo == "MOG":
    backSub = cv.bgsegm.createBackgroundSubtractorMOG()
else:
    print("Error: Target BGS algorithm not found!")
    exit(1)
## [create]

## [capture]
capture = cv.VideoCapture(cv.samples.findFileOrKeep(args.input))
if not capture.isOpened():
    print('Unable to open: ' + args.input)
    exit(0)
## [capture]

count = 0

frame_array = []
size = (1280, 360)

while True:
    ret, frame = capture.read()
    if frame is None:
        break

    ## [apply]
    #update the background model
    fgMask = backSub.apply(frame)
    ## [apply]

    ## [display_frame_number]
    #get the frame number and write it on the current frame
    cv.rectangle(frame, (10, 2), (100,20), (255,255,255), -1)
    cv.putText(frame, str(capture.get(cv.CAP_PROP_POS_FRAMES)), (15, 15),
               cv.FONT_HERSHEY_SIMPLEX, 0.5 , (0,0,0))
    ## [display_frame_number]

    # cv.imwrite(f"result/img-{count}.png", fgMask)
    combined_frame = np.stack((fgMask,)*3, axis=-1)
    combined_frame = np.concatenate((frame, combined_frame), axis=1)
    frame_array.append(combined_frame)
    ## [show]

    count += 1

    keyboard = cv.waitKey(30)
    # if keyboard == 'q' or keyboard == 27 or count == 60:
    if keyboard == 'q' or keyboard == 27:
        break

out = cv.VideoWriter('mog_mask1000.mp4', cv.VideoWriter_fourcc(*'mp4v'), 30, size) # last parameter set color to gray scale
for i in range(len(frame_array)):
    out.write(frame_array[i])
out.release()