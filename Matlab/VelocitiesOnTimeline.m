work = true;
if(work)
    addpath \\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab
    filename = '\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab\angularVelocity.csv';
    filename_f = '\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv';
    fp = '\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab'; 
    fileID = fopen('\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv');
else
    addpath E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab
    filename = 'E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab\angularVelocity.csv';
    filename_f = 'E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv';
    fp = 'E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab';
    fileID = fopen('E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv');
end;

figure;

%To compare multiple files, just add another file to this cell array
%structure
B = { 
    %textscan( fopen(strcat(fp,'\walking90deg.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\windowWashing.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\GazeInferenceTestA.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    };
 
length(B{1}{3})

yOffset = 0;
height = 0.2;
%can only support 5 colors right now
colors = [ 'c', 'm', 'y', 'm', 'c' ]; 
%colors =  [0.98 0.70 0.68]

%ylim( [0, 8] );
%set(gca, 'YTickLabel', '');
%set(gca, 'Ytick', '');

for i = 1:length(B)
    for j = 1:length(B{i}{3})
       rectangle('Position', [B{i}{3}(j), yOffset, ( B{i}{4}(j) - B{i}{3}(j) ), height], ...
                 'FaceColor', colors(i),...
                 'Curvature', [0.1 0.1]);
             x = double(B{i}{3}(j));
       text(x+2, yOffset + height/2, B{i}{6}(j));
       string = strcat( int2str(B{i}{3}(j)), ', ', int2str(B{i}{4}(j))   );
       text(x+2, yOffset + height/4, string); 
       
    end;
    yOffset = yOffset + .01;
end;
axdrag();
hold on;


%%
magnitudes_f = csvread(filename_f,1,0);

plot(magnitudes_f(:,1));
hold on;
plot(magnitudes_f(:,2));
ax = gca;
ax.XTick = 0:10:length(magnitudes);
m = max(max(magnitudes_f));
a = mean(mean(magnitudes_f));

legend('Head', 'Chest', 'SpineA', 'SpineB', 'Hips');


%draw vertical lines
yL = get(gca, 'YLim');

C = textscan(fileID, '%s', 1);
title(strcat(C{1}, ' Angular Velocity Magnitudes Filtered'));

if(strcmp('Walking90deg', C{1}))
    fid = fopen(strcat(fp,'\walking90deg.csv'));
elseif(strcmp('WindowWashingA', C{1}))
    fid = fopen(strcat(fp,'\windowWashing.csv'));
elseif(strcmp('PassSodaA', C{1}))
    fid = fopen(strcat(fp,'\PassSodaA.csv'));
elseif(strcmp('PassSodaB', C{1}))
    fid = fopen(strcat(fp,'\PassSodaB.csv'));
end

A = textscan(fid, '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);

%y location where frame numbers will be displayed
%y = 0.02;


%for n = 1:length(A{4})
    %x1 = line([A{4}(n) A{4}(n)], [yL(1,1) yL(1,2)], 'LineStyle', ':', 'Color', 'r', 'LineWidth', 1.1);
    %x2 = line([A{3}(n) A{3}(n)], [yL(1,1) yL(1,2)], 'LineStyle', ':', 'Color', 'r', 'LineWidth', 1.1);
    %text of the number
    %str = num2str(A{3}(n));
    %x = double(A{3}(n));
    %text( x, y, str);
%end

axdrag();