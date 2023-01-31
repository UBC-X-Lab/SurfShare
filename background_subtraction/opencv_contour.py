from tokenize import Imagnumber
import cv2 as cv
import numpy as np

im = cv.imread('holes_holes_mask.jpg')
imgray = cv.cvtColor(im, cv.COLOR_BGR2GRAY)

ret, thresh = cv.threshold(imgray, 127, 255, 0)
contours, hierarchy = cv.findContours(thresh, cv.RETR_CCOMP, cv.CHAIN_APPROX_SIMPLE)

print(len(contours))

# put each objects into the contours and holes
objs_indices = []

for i, hier in enumerate(hierarchy[0]):
    if hier[3] == -1: # is outer contour
        new_obj = [i]
        if hier[2] != -1: # has child
            hole_index = hier[2]
            while True:
                new_obj.append(hole_index)
                hier_hole = hierarchy[0][hole_index]
                hole_index = hier_hole[0]
                if hole_index == -1:
                    break
        objs_indices.append(new_obj)

print(objs_indices)


res_cons = []

for obj in objs_indices:
    res_con = []
    # print(obj)
    for i, cnt_index in enumerate(obj):
        cnt = contours[cnt_index]
        thresh = (2 if i == 0 else 1)
        area = cv.contourArea(cnt)
        if area > thresh:
            # contour approx
            epsilon = 0.005*cv.arcLength(cnt,True) # the second parameter is set to true, meaning that it is a closed contour
            approx = cv.approxPolyDP(cnt,epsilon,True) # the third parameter is set to true, meaning that it is a closed contour
            res_con.append(approx)
            print(f"Area {area}")
            print(f"Arc length {len(approx)}")
        elif i == 0:
            break
    if len(res_con) > 0:
        res_cons.append(res_con)

# # contour approx
# approx_con = []
# for cnt in res_con:
#     epsilon = 0.005*cv.arcLength(cnt,True) # the second parameter is set to true, meaning that it is a closed contour
#     approx = cv.approxPolyDP(cnt,epsilon,True) # the third parameter is set to true, meaning that it is a closed contour
#     approx_con.append(approx)
#     print(f"Arc length {len(approx)}") # length is the number of vertices

# res = cv.drawContours(im, res_con, -1, (0,255,0), 3)

for i in range(len(res_cons)):
    res = cv.drawContours(im, res_cons[i], -1, (0,255,0), 3)
    cv.imshow("image", res)
    cv.waitKey(0)


# for con in contours:
#     print(len(con))


# cv.destroyAllWindows()