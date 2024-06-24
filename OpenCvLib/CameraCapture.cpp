#include "CameraCapture.h"
#include <opencv2/opencv.hpp>
#include <cstring>
#include <iostream>

cv::VideoCapture cap;
cv::Mat currentFrame;
cv::Point lineStart, lineEnd;
bool lineSet = false;
bool objectCrossedLine = false;
const int desiredWidth = 1920;
const int desiredHeight = 1080;


extern "C" __declspec(dllexport) void InitializeCamera();
extern "C" __declspec(dllexport) void ReleaseCamera();
extern "C" __declspec(dllexport) bool GetFrame(unsigned char* buffer, int* width, int* height);
extern "C" __declspec(dllexport) void SetLineCoordinates(int x1, int y1, int x2, int y2);

void InitializeCamera() {
	cap.open(0);
	if (!cap.isOpened()) {
		std::cerr << "Error: Camera could not be opened." << std::endl;
	}

	if (!cap.isOpened()) {
		std::cerr << "Camera could not be opened!" << std::endl;
		return;
	}
}

void ReleaseCamera() {
	if (cap.isOpened()) {
		cap.release();
	}
}

bool GetFrame(unsigned char* buffer, int* width, int* height) {
	if (!cap.isOpened()) {
		return false;
	}

	cap >> currentFrame;
	if (currentFrame.empty()) {
		return false;
	}

	// Draw the line if coordinates are set
	if (lineSet) {
		cv::line(currentFrame, lineStart, lineEnd, cv::Scalar(0, 255, 0), 2);
	}

	//// Redimensionează frame-ul la dimensiunile dorite
	//cv::Mat resizedFrame;
	//cv::resize(currentFrame, resizedFrame, cv::Size(desiredWidth, desiredHeight));

	*width = currentFrame.cols;
	*height = currentFrame.rows;
	int bufferSize = currentFrame.total() * currentFrame.elemSize();
	std::memcpy(buffer, currentFrame.data, bufferSize);
	
	return true;
}

void SetLineCoordinates(int x1, int y1, int x2, int y2) {
	lineStart = cv::Point(x1, y1);
	lineEnd = cv::Point(x2, y2);
	lineSet = true;
}


