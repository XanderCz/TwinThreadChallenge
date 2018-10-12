# TwinThread Challenge

A sample coding project for TwinThread. Prompt may be found [here:](https://slack-files.com/T3STCTQ3G-FCYMKERS6-18b2d13bad)
Project written in C# using Visual Studio IDE.

### Installing

Download the release package from Github. The project is a Command Line program, so simply navigate to the repository in Command Prompt and run.

### Usage

If run without any tags, the program will simply download the asset file and then wait for keypress to exit.
Tags:
	-s= | -search= : your search terms. Format is -s=field:SearchValue. If using multiple property search, format is -s="field1:SearchValue1 field2:SearchValue2"
		Field is never case sensitive, but SeachValue is.
		Search supports wildcard search with a single wildcard only. Wildcard may appear anywhere within the search value.
			Examples:
				-s=name:A* - returns assets with name starting with 'A'
				-search=name:*test - returns assets with name ending with 'test'
				-search=description:Asset*56 - returns assets with description starting with 'Asset' and ending with '56'
		Search supports range search for integer fields. Mixing inclusive/exclusive brackets is not supported.
			Examples:
				-s=status:[1-2] - INCLUSIVE range. returns assets with status 1 or 2.
				-s=status:{1-3} - EXCLUSIVE range. returns assets with status 2.
		Search supports multiple property search, but you must include a multisearch tag. Multisearch tag can be AND / OR. Only two search terms are supported in multisearch. 
			Examples:
				-s="status:3 status:1" -mt=or - returns assets with status 1 or status 3.
				-s="assetid:[10000-10324] name:newtwin*" -mt=and - returns assets with ID in range [10000-10324] (inclusive) AND name beginning with 'newtwin'
	-c | -criticals : Use this tag to quickly search for all assets with a critical status. NOT COMPATIBILE WITH -s TAG.
	-u | -uniques : Use this tag to get a count of unique class names, then for each, list the names of assets that have those class names. User is prompted if they want to print the list of assets.
	-mt= | -multitag= : REQUIRED if performing a multiple property search. Tags are AND / OR. See above for examples.
	-hi | -hierarchy : If a search returns a single asset, this tag will display a tree of assets with the search results as the root node. Does nothing if a search returns multiple assets.
	-v : Sets verbose. Search results will print entire Asset objects, instead of just the name of the Asset.

	If searching the fields: Running, Utilization, Performance, Location - search term is compared with the Field.name
	If searching the field ClassList - search term is compared to classList[i].name



## Authors

* **Xander Czajkowski** - *Initial work* - [GitHub](https://github.com/XanderCz)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

Liberal use of Google and Stack Overflow.
