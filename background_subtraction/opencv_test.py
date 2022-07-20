#from __future__ import print_function
import cv2 as cv
import argparse
import numpy as np
import time

def frame_rgb_to_hsv_fast(frame):
    # start = time.time()
    rgb_frame = frame.reshape((frame.shape[0] * frame.shape[1], frame.shape[2]))
    R = rgb_frame[:, 0]
    G = rgb_frame[:, 1]
    B = rgb_frame[:, 2]
    x_max = np.max(rgb_frame, axis=1) # this is also V
    x_min = np.min(rgb_frame, axis=1)
    C = x_max - x_min
    H_R = 1 / 6 * ((G - B) / C)
    np.nan_to_num(H_R, copy=False)
    H_G = 1 / 6 * (2 + (B - R) / C)
    np.nan_to_num(H_G, copy=False)
    H_B = 1 / 6 * (4 + (R - G) / C)
    np.nan_to_num(H_B, copy=False)
    np.put(H_R, np.where((x_max == R) == False), 0)
    np.put(H_G, np.where((x_max == G) == False), 0)
    np.put(H_B, np.where((x_max == B) == False), 0)
    H = H_R + H_G + H_B
    S = C / x_max
    np.nan_to_num(S, copy=False)
    H = H.reshape(-1, 1)
    S = S.reshape(-1, 1)
    x_max = x_max.reshape(-1, 1)
    # hsv_frame = np.concatenate((H, S, np.full(x_max.shape, 0)), axis=1).reshape(frame.shape)
    hsv_frame = np.concatenate((H, S, x_max), axis=1).reshape(frame.shape)
    # print(hsv_frame)
    # print(f"color convertion took {time.time() - start}")
    return hsv_frame

def main():
    parser = argparse.ArgumentParser(description='This program shows how to use background subtraction methods provided by \
                                                OpenCV. You can process both videos and images.')
    parser.add_argument('--input', type=str, help='Path to a video or a sequence of image.', default='test_images/real_test1.mp4')
    parser.add_argument('--algo', type=str, help='Background subtraction method (KNN, MOG2).', default='MOG2')
    args = parser.parse_args()

    ## [create]
    #create Background Subtractor objects
    if args.algo == 'MOG2':
        backSub = cv.createBackgroundSubtractorMOG2(detectShadows=False)
        backSub.setVarThreshold(200) # default is 16
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

    start = time.time()

    while True:
        ret, frame = capture.read()
        if frame is None:
            break

        # original background subtraction
        # fgMask = backSub.apply(frame)
        
        ## convert to HSV space
        hsv_frame = frame_rgb_to_hsv_fast(frame)

        ## [apply]
        # update the background model
        if count < 90:
            fgMask = backSub.apply(hsv_frame)
        else:
            fgMask = backSub.apply(hsv_frame, 0, 0.0005)
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

        print(count)

        keyboard = cv.waitKey(30)
        if keyboard == 'q' or keyboard == 27:
        # if keyboard == 'q' or keyboard == 27:
            break

    print(f"process speed: {count / (time.time() - start)}")

    # out = cv.VideoWriter('mog_mask1000.mp4', cv.VideoWriter_fourcc(*'mp4v'), 30, size) # last parameter set color to gray scale
    out = cv.VideoWriter('test_desk_zero_update_rate.mp4', cv.VideoWriter_fourcc(*'mp4v'), 30, size) # last parameter set color to gray scale
    for i in range(len(frame_array)):
        out.write(frame_array[i])
    out.release()

if __name__ == "__main__":
    main()