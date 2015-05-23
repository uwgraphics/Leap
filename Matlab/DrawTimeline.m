

fp = strcat(pwd, '\')
addpath pwd


%animation title
iFile = textscan( fopen(strcat(fp,'\GazeInference_BlockMatch.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
CurrAnim_full = iFile{2}{1};
CurrAnimation = CurrAnim_full(1:length(CurrAnim_full)-5)

CurrAnimationCSV = strcat(CurrAnimation, '.csv');

%labels
%Labels = {'Old System', 'SpineB', 'SpineA', 'Hips', 'Head', 'Chest', 'HeadLocal', 'BlockMatch'};
Labels = {'Chest', 'HeadLocalOrig','HeadLocal2.5', 'BlockMatchOrig', 'BlockMatch'};

%To compare multiple files, just add another file to this cell array
%structure
B = { 
    %textscan( fopen(strcat(fp,'\walking90deg.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    
    %textscan( fopen(strcat(fp,'\GazeInferenceTestA.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    %textscan( fopen(strcat(fp,'\GazeInferenceTestB.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    %textscan( fopen(strcat(fp,'\GazeInferenceTestD.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1)
    textscan( fopen(strcat(fp,'\GazeInference_Chest.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\GazeInference_HeadLocalOrig.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\GazeInference_HeadLocal.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\GazeInference_BlockMatchOrig.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\GazeInference_BlockMatch.csv')), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp, CurrAnimationCSV)), '%s%s%d%d%d%s%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    };

length(B{1}{3})

yOffset = 0;
height = 1;

%can only support 5 colors right now
colors = [ 'c', 'm', 'y', 'g', 'c', 'r', 'b', 'g', 'y' ]; 
%colors =  [0.98 0.70 0.68]

figure;

title(strcat(CurrAnimation, ' Timeline'));
ylim( [0, 8] );
set(gca, 'YTickLabel', '');
set(gca, 'Ytick', '');



for i = 1:(length(B)-1)
    for j = 1:length(B{i}{3})
       rectangle('Position', [B{i}{3}(j), yOffset, ( B{i}{4}(j) - B{i}{3}(j)), height], ...
                 'FaceColor', colors(i),...
                 'Curvature', [0.1 0.1]);
             x = double(B{i}{3}(j));
       text(x+2, yOffset + height/2, B{i}{6}(j));
       string = strcat( int2str(B{i}{3}(j)), ', ', int2str(B{i}{4}(j))   );
       text(x+2, yOffset + height/4, string); 
       
    end;
    %label
    text(-40, yOffset + height/4, Labels(i))
    yOffset = yOffset + 1; 
end;

%just the hand annotations with relative offset
h = length(B);
for j = 1:length(B{h}{3})
    f = B{h}{1}(j);
    if(strcmp(f{1}(1:1), '#'))
        continue;
    end;
rectangle('Position', [B{h}{3}(j), yOffset, ( B{h}{4}(j) ), height], ...
          'FaceColor', colors(h),...
          'Curvature', [0.1 0.1]);
           x = double(B{h}{3}(j));
       text(x+2, yOffset + height/2, B{h}{6}(j));
       endFrame = B{h}{3}(j) + B{h}{4}(j);
       string = strcat( int2str(B{h}{3}(j)), ', ', int2str(endFrame)  );
       text(x+2, yOffset + height/4, string); 
      
end;      
axdrag();  
    