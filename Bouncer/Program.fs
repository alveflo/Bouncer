open System
open System.Net
open System.Net.Http
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

let deserialize<'T> (httpResponse: HttpResponseMessage) =
    async {
        let! content = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
        return JsonConvert.DeserializeObject<'T> content
    }

let get<'T> (url: string) =
    async {
        printfn "Getting %s" url
        let httpClient = new HttpClient()
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask

        printfn "Received status code %s from %s" (response.StatusCode.ToString()) url
        if response.StatusCode <> HttpStatusCode.OK then
            return None
        else
            let! result = deserialize<'T> response
            return Some(result)
    }

let getLatestVersion package =
    async {
        let! nugetVersions = get<NugetVersions> ("https://api.nuget.org/v3-flatcontainer/" + package + "/index.json")

        if nugetVersions.IsNone then
            return None
        else
            return Some(Seq.last nugetVersions.Value.Versions)
    }

let getLicenseUrl package version =
    async {
        printfn "Getting license url for %s" package
        let! registrationInfo = get<NugetRegistrationInfo> ("https://api.nuget.org/v3/registration3/" + package.ToLower() + "/" + version + ".json")
        if registrationInfo.IsNone then
            return None
        else
            let! catalogInfo = get<NugetCatalogInfo> registrationInfo.Value.CatalogEntry
            if catalogInfo.IsNone then
                return None
            else
                return Some(catalogInfo.Value.LicenseUrl)
    }

[<EntryPoint>]
let main argv =
    let deps = getDependencies "sample.xml"
    let a = (deps
        |> Seq.map (fun d -> d.Name + ": " + d.Version)
        |> String.concat Environment.NewLine)

    let dep = Seq.last deps
    let v = getLatestVersion dep.Name |> Async.RunSynchronously

    if v.IsNone then
        printfn "Unable to resolve version for package %s" dep.Name
    else
        let licenseUrl = getLicenseUrl dep.Name dep.Version |> Async.RunSynchronously
    
        if licenseUrl.IsNone then
            printfn "Unable to resolve license url"
        else
            printfn "%s" licenseUrl.Value
    0
