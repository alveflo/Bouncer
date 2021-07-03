open System
open System.Xml
open Newtonsoft.Json

type Dependency = { Name: string; Version: string;  }
type NugetVersions = { Versions: seq<string>  }
type NugetRegistrationInfo = { CatalogEntry: string }
type NugetCatalogInfo = { LicenseUrl: string  }

let getDependencies (filename: string) =
    let xmlDocument = new XmlDocument()
    xmlDocument.Load filename
    xmlDocument.SelectNodes "/Project/ItemGroup/PackageReference"
        |> Seq.cast<XmlNode>
        |> Seq.map (fun node -> { Name = node.Attributes.Item(0).Value; Version = node.Attributes.Item(1).Value })

let parseHtml html =
    let xmlDocument = new XmlDocument()
    xmlDocument.LoadXml html
    xmlDocument.SelectNodes "html"

let get<'T> (url: string) =
    async {
        let httpClient = new System.Net.Http.HttpClient()
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        response.EnsureSuccessStatusCode() |> ignore
        let! content = response.Content.ReadAsStringAsync()|> Async.AwaitTask

        return JsonConvert.DeserializeObject<'T> content
    }

let getLatestVersion package =
    async {
        let! nugetVersions = get<NugetVersions> ("https://api.nuget.org/v3-flatcontainer/" + package + "/index.json")

        return Seq.last nugetVersions.Versions
    }

let getLicenseUrl package version =
    async {
        let! registrationInfo = get<NugetRegistrationInfo> ("https://api.nuget.org/v3/registration3/" + package + "/" + version)
        let! catalogInfo = get<NugetCatalogInfo> registrationInfo.CatalogEntry

        return catalogInfo.LicenseUrl
    }

[<EntryPoint>]
let main argv =
    let deps = getDependencies "sample.xml"
    let a = (deps
        |> Seq.map (fun d -> d.Name + ": " + d.Version)
        |> String.concat Environment.NewLine)

    let latestVersion = getLatestVersion "Sentry.AspNetCore" |> Async.RunSynchronously
    let licenseUrl = getLicenseUrl "Sentry.AspNetCore" latestVersion |> Async.RunSynchronously
    printfn "%s" licenseUrl
    0
