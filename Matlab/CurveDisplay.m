%this loads in the prefiltered euler angles for displaying

work = false;
if(work)
   pre_filename = 'C:\Local Users\drakita\Documents\cs699\prefilter.csv';
   post_filename = 'C:\Local Users\drakita\Documents\cs699\postfilter.csv';
else
    pre_filename = 'C:\Users\Danny\Desktop\prefilter.csv';
    post_filename = 'C:\Users\Danny\Desktop\postfilter.csv';
end;

prefilter = csvread(pre_filename,1,0);
%x
figure;
plot(prefilter(:,1), ':', 'color', 'red');
hold on;
%y
plot(prefilter(:,2), ':', 'color', 'green');
hold on;
%z
plot(prefilter(:,3), ':', 'color', 'blue');
title('Prefiltered Motion');
hold on;

postfilter = csvread(post_filename,1,0);
%x
%figure;
plot(postfilter(:,1), 'color', 'red', 'LineWidth', 1.1);
hold on;
%y
plot(postfilter(:,2), 'color', 'green', 'LineWidth', 1.1);
hold on;
%z
plot(postfilter(:,3), 'color', 'blue', 'LineWidth', 1.1);
legend('pre-x','pre-y','pre-z', 'x', 'y', 'z');
title('Postfiltered Motion');

