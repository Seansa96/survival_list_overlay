   M S B A M L   `   `   `  ����P  Lsurvival_list_overlay, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=#clr-namespace:survival_list_overlaysurvival_list_overlay  5   	   &     survival_list_overlay.MainWindow   5   	   W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   5local#clr-namespace:survival_list_overlay     5   	   -   5          � Title$  
SG Overlay��5   	    � SizeToContent$ Height=�6   $	��300��63    � WindowStyle$
 None=�5
   	    � AllowsTransparency$
 True��5   	    � 
ResizeMode$ NoResize=�5   	   
������  � Topmost$
 True��5   	    � ShowInTaskbar$
 True��5   	   .��5      � -   5         �Name$ RootGrid��5      .+�5   
   �� -   5         ��Name$ TitleBar��5      ���	#66000000$��30��5      $	��Top=�5      	 �� CornerRadius$	 8,8,0,0��5      $y�	Collapsed=�5      ._�5      ]� .+�5      �� $��
SG Overlay��6   
�������$��Center=�5!      $
��10,0q�5"      �� 5#      -   5        $��X��5#      $��Right=�5$      $��30��5&      $��30��5'      $��0q�5(      5)      5*      �� 5-   
   ���	#AA111111$	 12��6(   
 �� Padding$
 10q�6:   $��0,30,0,0q�6G   ._�5.      �� .+�5/      �� ��6   �� *Items+��50      �� 51      .R�55      � $��5q�6#    � ColumnDefinitions 56   "   �� 57   &   $
��Auto �67   �� 58   &   $��* �67   .+�5:   "   �� 
�������$��CharacterEllipsis=�6U   $��NoWrap=�6v   $��12(�6�   $
��Bold"�6�   $��50��5;   ,   $	��120��6:   $��Center=�6I   $��0,0,10,0q�6d   ��5:   ,   �� *Name+5;   d   � 5>   "   $��1��6'   $��25��67   .+�5?   &   � $��15��6g    � Value 62   �� *
Progress+ � Maximum 6M   �� *Total+6g   �� 5@   &   $��Center=�60   $��Center=�6K   ��5A   *   `� 5B   .    �� StringFormat$ 	{0} / {1}��6;   .1�5C   2   ��  �� Path$ Progress�6:   �� 5D   2   $ Total�6:   5E   /   5G   '   �� 5J   &   $��2��61    �� Orientation$ 
Horizontal=�6A   $��10,0,0,0q�6Z   $��Center=�6l   .+�5K   *   �� $��-��61   $��15��6=   $��15��6H   ��6T   �� *DecrementCommand+�� 5L   *   $��+��61   $��15��6=   $��15��6H   $��5,0,0,0q�6y   ��6T   �� *IncrementCommand+6y   �� 5M   *   $	��Rem��61   $��40��6?   $��20��6J   $��10(�6V   $��Center=�6d   $��5,0,0,0q�5N   N   ��5M   �   �� *DataContext.RemoveItemCommand+ �� RelativeSource ��  �� AncestorType
 Window��5N   1   �� 6N   5O   '   5Q   #   5S      5T      5V      �� 5Y      $��5q�6   �� 5\      $ 
Horizontal=�6   $��0,5,0,0q�66   .+�5]      �� .+�5^      �� $��	Item Name��6$   
�������$��0,0,0,2q�6H   �� 5_      $	��100��6"   ��6.   �� *NewItemName+ �� UpdateSourceTrigger$ PropertyChanged=�5`      �� 5b      $��5,0,0,0q�6!   .+�5c      �� $��Total��6$   
�������$��0,0,0,2q�6D   �� 5d      $��50��6"   ��6-   �� *NewItemTotal+$ PropertyChanged=�5e      �� 5g      $	��Add��6   $��5,18,0,0q�6+   ��6=   �� *AddNewItemCommand+5h      5j      5k      5l      5m      