# <img src="assets/logo.png" height="30px"> Record Enumerable Equality - Fody

[![NuGet](https://img.shields.io/nuget/v/RecordEnumerableEquality.Fody.svg)](https://www.nuget.org/packages/RecordEnumerableEquality.Fody/)
<!-- [![Build Status](https://img.shields.io/github/workflow/status/JKamsker/RecordEnumerableEquality.Fody/CI)](https://github.com/JKamsker/RecordEnumerableEquality.Fody/actions) -->

## Overview

`RecordEnumerableEquality.Fody` is a Fody add-in that enables deep equality comparison for C# records containing properties implementing `IEnumerable<T>`. This allows for comparison of records with collections in a straightforward and intuitive manner, ensuring that two records with the same collection elements are considered equal, even if the instances of the collections are different.

## Installation

To install `RecordEnumerableEquality.Fody`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```shell
Install-Package RecordEnumerableEquality.Fody
```

Or add the package to your project file:

```xml
<PackageReference Include="RecordEnumerableEquality.Fody" Version="x.y.z" />
```

## Usage

To use `RecordEnumerableEquality.Fody`, simply define your records with properties that implement `IEnumerable<T>`, and the Fody add-in will handle the deep equality comparison and hashing for you.

### Example

Here is an example demonstrating the functionality:

```csharp
var mc = new MainClass()
{
    Property1 = "Hello",
    SubClasses = new List<SubClass>()
    {
        new ()
        {
            Property2 = "Hello",
            Property3 = "World!"
        },
        new ()
        {
            Property2 = "Hello1",
            Property3 = "World2!"
        },
    }
};

var mc1 = new MainClass()
{
    Property1 = "Hello",
    SubClasses = new List<SubClass>()
    {
        new ()
        {
            Property2 = "Hello",
            Property3 = "World!"
        },
        new ()
        {
            Property2 = "Hello1",
            Property3 = "World2!"
        },
    }
};

Console.WriteLine($"Equals: {mc.Equals(mc1)}");

var hashCode = mc.GetHashCode();
var hashCode1 = mc1.GetHashCode();

Console.WriteLine($"HashCode1: {hashCode}");
Console.WriteLine($"HashCode2: {hashCode1}");
Console.WriteLine($"HashCode Equals: {hashCode == hashCode1}");

public record MainClass
{
    public string Property1 { get; set; }

    public List<SubClass> SubClasses { get; set; } = new List<SubClass>();
}

public record SubClass
{
    public string Property2 { get; set; }
    public string Property3 { get; set; }
}
```

### Output

```text
Equals: True

HashCode1: 12345678
HashCode2: 12345678
HashCode Equals: True
```

Without the Add-in, the output would be not equal and the hash codes would be different.

## Contributing

Contributions are welcome! If you have any ideas, suggestions, or bug reports, please open an issue or submit a pull request.

1. Fork the repository.
2. Create a new feature branch.
3. Commit your changes.
4. Push the branch.
5. Open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Acknowledgements

Special thanks to the [Fody](https://github.com/Fody/Fody) community for their support and contributions.
