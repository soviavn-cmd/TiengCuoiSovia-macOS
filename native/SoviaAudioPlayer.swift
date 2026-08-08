import AVFoundation
import Foundation

final class PlaybackDelegate: NSObject, AVAudioPlayerDelegate {
    func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
        exit(flag ? 0 : 2)
    }

    func audioPlayerDecodeErrorDidOccur(_ player: AVAudioPlayer, error: Error?) {
        if let error { FileHandle.standardError.write(Data("\(error)\n".utf8)) }
        exit(3)
    }
}

guard CommandLine.arguments.count == 3,
      let initialVolume = Float(CommandLine.arguments[2]) else {
    FileHandle.standardError.write(Data("Usage: SoviaAudioPlayer <audio-path> <volume-0-to-1>\n".utf8))
    exit(64)
}

do {
    let player = try AVAudioPlayer(contentsOf: URL(fileURLWithPath: CommandLine.arguments[1]))
    let playbackDelegate = PlaybackDelegate()
    player.delegate = playbackDelegate
    player.volume = min(max(initialVolume, 0), 1)
    player.prepareToPlay()
    guard player.play() else { exit(4) }

    DispatchQueue.global(qos: .userInitiated).async {
        while let command = readLine() {
            let parts = command.split(separator: " ", maxSplits: 1).map(String.init)
            if parts.first == "stop" {
                DispatchQueue.main.async { player.stop(); exit(0) }
                return
            }
            if parts.count == 2, parts[0] == "volume", let value = Float(parts[1]) {
                DispatchQueue.main.async { player.volume = min(max(value, 0), 1) }
            }
        }
    }

    RunLoop.main.run()
} catch {
    FileHandle.standardError.write(Data("\(error)\n".utf8))
    exit(1)
}
