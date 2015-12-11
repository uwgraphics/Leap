% Plot configuration:
boneMask = [];
endEffectorMask = [];
showDRoot = false;
showDBones = false;
showARoot = false;
showABones = false;
showP0Root = false;
showP0Bones = false;
showPRoot = true;
showPBones = true;
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

% Norman bone indexes:
%
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
% dataPerFrame column index ranges:
%
% 1         dRoot
% 2:19      dBones
% 20        aRoot
% 21:38     aBones
% 39        p0Root
% 40:57     p0Bones
% 58        pRoot
% 59:76     pBones
% 77        wRoot
% 78:95     wBones
% 96:99     pEndEff
% 100:103   wEndEff
% 104       p0
% 105       p
%
% dataPerKey column index ranges:
%
% 1         keyFrame
% 2         rootKeyFrame
% 3:20      boneKeyFrame

% Load per-frame data
dataPerFrame = csvread('dataPerFrame.csv', 1);
frameLength = size(dataPerFrame, 1);
frames = [startFrame:endFrame];
dRoot = dataPerFrame(startFrame:endFrame, 1);
dBones = dataPerFrame(startFrame:endFrame, 2:19);
aRoot = dataPerFrame(startFrame:endFrame, 20);
aBones = dataPerFrame(startFrame:endFrame, 21:38);
if normalizeA
    aRoot = aRoot / max(aRoot);
    for i = 1:size(aBones, 2)
        aBonesSub = aBones(:, i);
        aBones(:, i) = aBonesSub / max(aBonesSub);
    end
end
p0Root = dataPerFrame(startFrame:endFrame, 39);
p0Bones = dataPerFrame(startFrame:endFrame, 40:57);
pRoot = dataPerFrame(startFrame:endFrame, 58);
pBones = dataPerFrame(startFrame:endFrame, 59:76);
wRoot = dataPerFrame(startFrame:endFrame, 77);
wBones = dataPerFrame(startFrame:endFrame, 1);
pEndEff = dataPerFrame(startFrame:endFrame, 96:99);
wEndEff = dataPerFrame(startFrame:endFrame, 100:103);
p0 = dataPerFrame(startFrame:endFrame, 104);
p = dataPerFrame(startFrame:endFrame, 105);

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
        for i = 1:size(showPEndEff, 2)
            plot(frames, showPEndEff, '-c');
        end
    else
        for i = 1:size(endEffectorMask, 2)
            plot(frames, showPEndEff(:,endEffectorMask(:, i)), '-c');
        end
    end
end
if showWEndEff
    if size(endEffectorMask, 2) == 0
        for i = 1:size(showWEndEff, 2)
            plot(frames, showWEndEff, '-c');
        end
    else
        for i = 1:size(endEffectorMask, 2)
            plot(frames, showWEndEff(:,endEffectorMask(:, i)), '-c');
        end
    end
end
if showP0
    plot(frames, p0, '-m');
end
if showP
    plot(frames, p, '-r');
end

% Load per-key data
dataPerKey = csvread('dataPerKey.csv', 1);
keyFrames = dataPerKey(:, 1);
keyFrameIndexes = find(keyFrames < startFrame | keyFrames > endFrame);
keyFrames(keyFrameIndexes) = [];
rootKeyFrames = dataPerKey(keyFrameIndexes, 2);
boneKeyFrames = dataPerKey(keyFrameIndexes, 3:20);

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
