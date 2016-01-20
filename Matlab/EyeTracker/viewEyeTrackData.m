% Configure eye track data settings
videoDirectory = './EyeTrackMocapTest1-1';
videoWidth = 1280;
videoHeight = 960;

% Load video
frameFiles = dir(fullfile(videoDirectory, 'frame*.png'));
numFrames = length(frameFiles);
for frameIndex = 1 : numFrames
    frameFileName = strcat(videoDirectory, '/', frameFiles(frameIndex).name);
    video(:,:,:,frameIndex) = imread(frameFileName);
end

% Load eye tracking data

