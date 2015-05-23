

addpath pwd
filename = strcat(pwd, '\angularVelocity.csv');
filename_f = strcat(pwd, '\angularVelocityFiltered.csv');
fp = pwd;
fileID = fopen(strcat(pwd, '\angularVelocityFiltered.csv'));

figure; 

magnitudes = csvread(filename,1,0);

plot(magnitudes);
ax = gca;
ax.XTick = 0:20:length(magnitudes);

legend('Head', 'Chest', 'SpineA', 'SpineB', 'Hips');
title('Angular Velocity Magnitudes');

figure;

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
elseif(strcmp('StealDiamond', C{1}))
    fid = fopen(strcat(fp,'\StealDiamond.csv'));
elseif(strcmp('BookShelf', C{1}))
    fid = fopen(strcat(fp,'\BookShelf.csv'));
elseif(strcmp('WaitForBus', C{1}))
    fid = fopen(strcat(fp,'\WaitForBus.csv'));
elseif(strcmp('HandShakeA', C{1}))
    fid = fopen(strcat(fp,'\HandShakeA.csv'));
elseif(strcmp('HandShakeB', C{1}))
    fid = fopen(strcat(fp,'\HandShakeB.csv'));
end

A = textscan(fid, '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);

%y location where frame numbers will be displayed
y = 0.02;

for n = 1:length(A{4})
    x1 = line([A{4}(n) A{4}(n)], [yL(1,1) yL(1,2)], 'LineStyle', ':', 'Color', 'r', 'LineWidth', 1.1);
    x2 = line([A{3}(n) A{3}(n)], [yL(1,1) yL(1,2)], 'LineStyle', ':', 'Color', 'g', 'LineWidth', 1.1);
    %text of the number
    str = num2str(A{4}(n));
    x = double(A{4}(n));
    text( x, y, str);
end

axdrag();



