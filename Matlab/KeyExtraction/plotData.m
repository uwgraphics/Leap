% Plot configuration:
boneMask = [1];
endEffectorMask = [];
numberOfBones = 6;
numberOfEndEffectors = 4;
showDRoot = false;
showDBones = false;
showARoot = false;
showABones = false;
showP0Root = false;
showP0Bones = false;
showPRoot = false;
showPBones = false;
showWRoot = false;
showWBones = false;
showPEndEff = false;
showWEndEff = false;
showP0 = false;
showP = true;
showKeyFrames = true;
showRootKeyFrames = false;
showBoneKeyFrames = false;
startFrame = 1;
endFrame = 300;
normalizeA = true;

% Bone indexes:
%
% Norman
% 1. srfBind_Cn_Pelvis	 
% 2. srfBind_Cn_SpineA
% 3. srfBind_Cn_SpineB
% 4. srfBind_Cn_SpineC
% 5. srfBind_Cn_Head
% 6. srfBind_Lf_ArmA
% 7. srfBind_Lf_ArmD
% 8. Hand_L
% 9. srfBind_Rt_ArmA
% 10. srfBind_Rt_ArmB
% 11. srfBind_Rt_ArmD
% 12. Hand_R
% 13. srfBind_Lf_LegA
% 14. srfBind_Lf_LegC
% 15. srfBind_Lf_FootA
% 16. srfBind_Rt_LegA
% 17. srfBind_Rt_LegC
% 18. srfBind_Rt_FootA
%
% NormanNew (gaze only)
% 1. Bone_Hips
% 2. Bone_SpineA
% 3. Bone_SpineB
% 4. Bone_SpineC
% 5. Bone_Neck
% 6. Bone_Head
%
% End-effector indexes:
%
% 1. LWrist
% 2. RWrist
% 3. LFoot
% 4. RFoot

% Compute array index ranges for per-frame data
indexDRoot = 1;
startIndexDBones = indexDRoot + 1;
endIndexDBones = startIndexDBones + numberOfBones - 1;
indexARoot = endIndexDBones + 1;
startIndexABones = indexARoot + 1;
endIndexABones = startIndexABones + numberOfBones - 1;
indexP0Root = endIndexABones + 1;
startIndexP0Bones = indexP0Root + 1;
endIndexP0Bones = startIndexP0Bones + numberOfBones - 1;
indexPRoot = endIndexP0Bones + 1;
startIndexPBones = indexPRoot + 1;
endIndexPBones = startIndexPBones + numberOfBones - 1;
indexWRoot = endIndexPBones + 1;
startIndexWBones = indexWRoot + 1;
endIndexWBones = startIndexWBones + numberOfBones - 1;
startIndexPEndEff = endIndexWBones + 1;
endIndexPEndEff = startIndexPEndEff + numberOfEndEffectors - 1;
startIndexWEndEff = endIndexPEndEff + 1;
endIndexWEndEff = startIndexWEndEff + numberOfEndEffectors - 1;
indexP0 = endIndexWEndEff + 1;
indexP = indexP0 + 1;

% Compute array index ranges for per-key data
indexKeyFrame = 1;
indexRootKeyFrame = indexKeyFrame + 1;
startIndexBoneKeyFrame = indexRootKeyFrame + 1;
endIndexBoneKeyFrame = startIndexBoneKeyFrame + numberOfBones - 1;

% Load per-frame data
dataPerFrame = csvread('dataPerFrame.csv', 1);
frameLength = size(dataPerFrame, 1);
frames = [startFrame:endFrame];
dRoot = dataPerFrame(startFrame:endFrame, indexDRoot);
dBones = dataPerFrame(startFrame:endFrame, startIndexDBones:endIndexDBones);
aRoot = dataPerFrame(startFrame:endFrame, indexARoot);
aBones = dataPerFrame(startFrame:endFrame, startIndexABones:endIndexABones);
p0Root = dataPerFrame(startFrame:endFrame, indexP0Root);
p0Bones = dataPerFrame(startFrame:endFrame, startIndexP0Bones:endIndexP0Bones);
pRoot = dataPerFrame(startFrame:endFrame, indexPRoot);
pBones = dataPerFrame(startFrame:endFrame, startIndexPBones:endIndexPBones);
wRoot = dataPerFrame(startFrame:endFrame, indexWRoot);
wBones = dataPerFrame(startFrame:endFrame, startIndexWBones:endIndexWBones);
pEndEff = dataPerFrame(startFrame:endFrame, startIndexPEndEff:endIndexPEndEff);
wEndEff = dataPerFrame(startFrame:endFrame, startIndexWEndEff:endIndexWEndEff);
p0 = dataPerFrame(startFrame:endFrame, indexP0);
p = dataPerFrame(startFrame:endFrame, indexP);

% Normalize accelerations
if normalizeA
    aRoot = aRoot / max(aRoot);
    for i = 1:size(aBones, 2)
        aBonesSub = aBones(:, i);
        aBones(:, i) = aBonesSub / max(aBonesSub);
    end
end

% Plot per-frame data
hold on;
if showDRoot
    plot(frames, dRoot, '-k');
end
if showDBones
    if size(boneMask, 2) == 0
        for i = 1:size(dBones, 2)
            plot(frames, dBones, '-k');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(frames, dBones(:,boneMask(:, i)), '-k');
        end
    end
end
if showARoot
    plot(frames, aRoot, '-b');
end
if showABones
    if size(boneMask, 2) == 0
        for i = 1:size(aBones, 2)
            plot(frames, aBones, '-b');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(frames, aBones(:,boneMask(:, i)), '-b');
        end
    end
end
if showP0Root
    plot(frames, p0Root, '-g');
end
if showP0Bones
    if size(boneMask, 2) == 0
        for i = 1:size(p0Bones, 2)
            plot(frames, p0Bones, '-g');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(frames, p0Bones(:,boneMask(:, i)), '-g');
        end
    end
end
if showPRoot
    plot(frames, pRoot, '-c');
end
if showPBones
    if size(boneMask, 2) == 0
        for i = 1:size(pBones, 2)
            plot(frames, pBones, '-c');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(frames, pBones(:,boneMask(:, i)), '-c');
        end
    end
end
if showWRoot
    plot(frames, pWoot, '-y');
end
if showWBones
    if size(boneMask, 2) == 0
        for i = 1:size(wBones, 2)
            plot(frames, wBones, '-y');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(frames, wBones(:,boneMask(:, i)), '-y');
        end
    end
end
if showPEndEff
    if size(endEffectorMask, 2) == 0
        for i = 1:size(pEndEff, 2)
            plot(frames, pEndEff, '-+c');
        end
    else
        for i = 1:size(endEffectorMask, 2)
            plot(frames, pEndEff(:,endEffectorMask(:, i)), '-c');
        end
    end
end
if showWEndEff
    if size(endEffectorMask, 2) == 0
        for i = 1:size(wEndEff, 2)
            plot(frames, wEndEff, '-+c');
        end
    else
        for i = 1:size(endEffectorMask, 2)
            plot(frames, wEndEff(:,endEffectorMask(:, i)), '-c');
        end
    end
end
if showP0
    plot(frames, p0, '-m');
end
if showP
    plot(frames, p, '-r');
end

% TODO: remove this
% Show gaze joint velocities
dataPerFrame = csvread('gazeJointVelocities.csv', 1);
vBones = dataPerFrame(startFrame:endFrame, 1:numberOfBones);
if size(boneMask, 2) == 0
    for i = 1:size(vBones, 2)
        plot(frames, vBones, '-b');
    end
else
    for i = 1:size(boneMask, 2)
        plot(frames, vBones(:,boneMask(:, i)), '-b');
    end
end
%

% Load per-key data
dataPerKey = csvread('dataPerKey.csv', 1);
keyFrames = dataPerKey(:, indexKeyFrame);
keyFrameIndexes = find(keyFrames < startFrame | keyFrames > endFrame);
keyFrames(keyFrameIndexes) = [];
rootKeyFrames = dataPerKey(:, indexRootKeyFrame);
rootKeyFrames(keyFrameIndexes) = [];
boneKeyFrames = dataPerKey(:, startIndexBoneKeyFrame:endIndexBoneKeyFrame);
boneKeyFrames(keyFrameIndexes, :) = [];

% Plot per-key data
if showKeyFrames
    plot(keyFrames, zeros(size(keyFrames, 1)), 'or');
end
if showRootKeyFrames
    plot(rootKeyFrames, zeros(size(rootKeyFrames, 1)), 'xm');
end
if showBoneKeyFrames
    if size(boneMask, 2) == 0
        for i = 1:size(boneKeyFrames, 2)
            plot(boneKeyFrames(:, i), zeros(size(boneKeyFrames(:, i), 1)), 'xm');
        end
    else
        for i = 1:size(boneMask, 2)
            plot(boneKeyFrames(:, boneMask(:, i)), zeros(size(boneKeyFrames(:, boneMask(:, i)), 1)), 'xm');
        end
    end
end
