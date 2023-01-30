from tokenize import Imagnumber
import cv2 as cv
import numpy as np

im = cv.imread('holes_mask.jpg')
imgray = cv.cvtColor(im, cv.COLOR_BGR2GRAY)

ret, thresh = cv.threshold(imgray, 127, 255, 0)
contours, hierarchy = cv.findContours(thresh, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)


# filter contours that are too small
res_con = []
for cnt in contours:
    area = cv.contourArea(cnt)
    if area > 2500:
        res_con.append(cnt)
        print(area)

# contour approx
approx_con = []
for cnt in res_con:
    epsilon = 0.005*cv.arcLength(cnt,True)
    approx = cv.approxPolyDP(cnt,epsilon,True)
    approx_con.append(approx)
    print(len(approx))

# res = cv.drawContours(im, res_con, -1, (0,255,0), 3)
res = cv.drawContours(im, approx_con, -1, (0,255,0), 3)


# for con in contours:
#     print(len(con))

cv.imshow("image", res)
cv.waitKey(0)
# cv.destroyAllWindows()