import cv2 as cv
import argparse
import numpy as np
import colorsys
import time

def frame_rgb_to_hsv(frame):
    # start = time.time()
    hsv_frame = np.zeros((frame.shape[0], frame.shape[1], frame.shape[2]), dtype=np.float) # lose the vue (lightness) channel
    for i in range(frame.shape[0]):
        for j in range(frame.shape[1]):
            hsv_color = colorsys.rgb_to_hsv(*frame[i, j]) # hsv: (1, 1, 255)
            hsv_frame[i, j, 0] = hsv_color[0]
            hsv_frame[i, j, 1] = hsv_color[1]
            hsv_frame[i, j, 2] = hsv_color[2] / 255
    # print(hsv_frame)
    # print(f"color convertion took {time.time() - start}")
    return hsv_frame

def frame_rgb_to_hsv_fast(frame):
    # start = time.time()
    rgb_frame = frame.reshape((frame.shape[0] * frame.shape[1], frame.shape[2])) / 255
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
    hsv_frame = np.concatenate((H.reshape(-1, 1), S.reshape(-1, 1), x_max.reshape(-1, 1)), axis=1).reshape(frame.shape)
    # print(hsv_frame)
    # print(f"color convertion took {time.time() - start}")
    return hsv_frame
    
def get_naive_mask(first_frame, hsv_frame, thresh=0.04):
    # start = time.time()
    diff = np.abs(hsv_frame - first_frame)
    shape = (diff.shape[0], diff.shape[1], diff.shape[2])
    diff[diff <= thresh] = 0
    diff[diff > thresh] = 1 # foreground
    diff = diff.reshape((shape[0] * shape[1], shape[2])).astype(np.float16)
    mask = np.multiply(diff[:, 0], diff[:, 1]) * 255
    # print(f"masking took{time.time() - start}")
    return mask.reshape((shape[0], shape[1])).astype(np.uint8)


def main():
    parser = argparse.ArgumentParser(description='This program shows how to use background subtraction methods provided by \
                                                OpenCV. You can process both videos and images.')
    parser.add_argument('--input', type=str, help='Path to a video or a sequence of image.', default='test_images/real_test1.mp4')
    args = parser.parse_args()

    capture = cv.VideoCapture(cv.samples.findFileOrKeep(args.input))
    if not capture.isOpened():
        print('Unable to open: ' + args.input)
        exit(0)

    count = 0
    frame_array = []
    size = (1280, 360)

    first_hsv_frame = None

    while True:
        ret, frame = capture.read()
        if frame is None:
            break
        
        # convert to hsv frame
        hsv_frame = frame_rgb_to_hsv_fast(frame)
        # take the first frame
        if count == 0:
            first_hsv_frame = hsv_frame
        mask = get_naive_mask(first_hsv_frame, hsv_frame)

        ## [display_frame_number]
        #get the frame number and write it on the current frame
        cv.rectangle(frame, (10, 2), (100,20), (255,255,255), -1)
        cv.putText(frame, str(capture.get(cv.CAP_PROP_POS_FRAMES)), (15, 15),
                cv.FONT_HERSHEY_SIMPLEX, 0.5 , (0,0,0))
        ## [display_frame_number]

        # cv.imwrite(f"result/img-{count}.png", fgMask)
        combined_frame = np.stack((mask,)*3, axis=-1)
        combined_frame = np.concatenate((frame, combined_frame), axis=1)
        frame_array.append(combined_frame)
        ## [show]

        count += 1
        print(count)

        keyboard = cv.waitKey(30)
        # if keyboard == 'q' or keyboard == 27 or count == 300:
        if keyboard == 'q' or keyboard == 27:
            break

    out = cv.VideoWriter('mask.mp4', cv.VideoWriter_fourcc(*'mp4v'), 30, size) # last parameter set color to gray scale
    for i in range(len(frame_array)):
        out.write(frame_array[i])
    out.release()

if __name__ == "__main__":
    main()