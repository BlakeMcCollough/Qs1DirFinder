Author: Blake McCollough
Contact: blakemccollough@yahoo.com
Description:
	Qs1DirFinder.exe reads through CustomerInfo.ini files from customers in a given folder. The purpose is to identify customers
	who may be in danger of corrupting data due to QS1 systems spread through multiple drives. For the application to be functional,
	the root folder must contain customer data where a CustomerInfo.ini file must exist. A start and end date must also be provided
	to avoid reading files from customers who may not be active anymore. The application creates output Diagnosis.txt,
	where the customer's name and directory information is displayed. Only customers that have multiple directories are
	displayed, since they are the ones in danger.